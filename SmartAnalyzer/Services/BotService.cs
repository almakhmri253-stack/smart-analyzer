using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SmartAnalyzer.Data;
using SmartAnalyzer.Models;
using SmartAnalyzer.ViewModels;

namespace SmartAnalyzer.Services;

public class BotService(ApplicationDbContext db)
{
    public async Task<BotCommandResult> ProcessAsync(BotCommandRequest req, string userId)
    {
        var msg   = req.Message.Trim();
        var state = req.State ?? "idle";

        var file = await db.UploadedFiles
            .FirstOrDefaultAsync(f => f.Id == req.FileId && f.UserId == userId);
        if (file == null) return Fail("الملف غير موجود أو ليس لديك صلاحية.");

        var columns = await db.FileColumns
            .Where(c => c.UploadedFileId == req.FileId)
            .OrderBy(c => c.ColumnIndex)
            .ToListAsync();

        return state switch
        {
            "add_waiting"    => await DoAddValues(req, file, columns, msg),
            "delete_confirm" => await DoDeleteConfirm(req, file, msg),
            "edit_confirm"   => await DoEditConfirm(req, file, msg),
            _                => await ParseCommand(msg, file, columns)
        };
    }

    // ── Parser ───────────────────────────────────────────────────────
    private async Task<BotCommandResult> ParseCommand(string msg, UploadedFile file, List<FileColumn> cols)
    {
        var lower    = msg.ToLower().Trim();
        var colNames = cols.Select(c => c.ColumnName).ToList();

        if (R(@"^(مساعدة|مساعده|help|\?)").IsMatch(lower))
            return HelpMsg(colNames);

        if (R(@"^(الاعمدة|الأعمدة|اعمده|اعمدة|columns?|cols?)$").IsMatch(lower))
            return ColsMsg(colNames);

        // ADD inline: أضف: col=val
        var addInline = R(@"^(?:اضف|أضف|add)\s*[:\-]\s*(.+)", RegexOptions.IgnoreCase).Match(msg);
        if (addInline.Success && addInline.Groups[1].Value.Contains('='))
            return await ExecAdd(addInline.Groups[1].Value, file, cols);

        // ADD – ask
        if (R(@"^(?:اضف|أضف|add)\b", RegexOptions.IgnoreCase).IsMatch(lower))
        {
            var sample = string.Join(", ", colNames.Take(3).Select(c => $"{c}=قيمة"));
            return new BotCommandResult
            {
                Reply     = $"📝 أدخل قيم الصف الجديد:\n`{sample}, ...`",
                NewState  = "add_waiting",
                StateData = [],
                Success   = true,
                ActionType = "prompt"
            };
        }

        // DELETE row: احذف الصف 5
        var delRow = R(@"^(?:احذف|حذف|delete)\s+(?:الصف|صف|row)\s*#?\s*(\d+)", RegexOptions.IgnoreCase).Match(msg);
        if (delRow.Success)
        {
            int idx = int.Parse(delRow.Groups[1].Value);
            var rec = await db.DataRecords.FirstOrDefaultAsync(r => r.UploadedFileId == file.Id && r.RowIndex == idx);
            if (rec == null) return Fail($"الصف {idx} غير موجود.");
            return new BotCommandResult
            {
                Reply     = $"⚠️ حذف الصف **{idx}**:\n{RowPreview(rec, 3)}\n\n(نعم / لا)",
                NewState  = "delete_confirm",
                StateData = new() { ["type"] = "row", ["rowIndex"] = idx.ToString() },
                Success   = true,
                ActionType = "confirm"
            };
        }

        // DELETE where: احذف حيث col=val
        var delWhere = R(@"^(?:احذف|حذف|delete)\s+(?:حيث|where)\s+(.+)$", RegexOptions.IgnoreCase).Match(msg);
        if (delWhere.Success)
        {
            var cond = delWhere.Groups[1].Value.Trim();
            var (cnt, err) = await CountMatch(file.Id, cond);
            if (err != null) return Fail(err);
            if (cnt == 0) return Ok("لم يُعثر على صفوف تطابق الشرط.");
            return new BotCommandResult
            {
                Reply     = $"⚠️ وُجد **{cnt}** صف يطابق: `{cond}`\nهل تريد حذفها؟ (نعم / لا)",
                NewState  = "delete_confirm",
                StateData = new() { ["type"] = "condition", ["condition"] = cond },
                Success   = true,
                ActionType = "confirm"
            };
        }

        // EDIT row: عدل الصف 5: col=val
        var editRow = R(@"^(?:عدل|edit)\s+(?:الصف|صف|row)\s*#?\s*(\d+)\s*[:\-]\s*(.+)$", RegexOptions.IgnoreCase).Match(msg);
        if (editRow.Success)
            return await ExecEditRow(int.Parse(editRow.Groups[1].Value), editRow.Groups[2].Value, file, cols);

        // EDIT col syntax: عدل الصف 5 عمود X إلى Y
        var editCol = R(@"^(?:عدل|edit)\s+(?:الصف|صف|row)\s*#?\s*(\d+)\s+(?:عمود|col(?:umn)?)?\s*(.+?)\s+(?:إلى|الى|to|=)\s*(.+)$", RegexOptions.IgnoreCase).Match(msg);
        if (editCol.Success)
            return await ExecEditRow(int.Parse(editCol.Groups[1].Value),
                $"{editCol.Groups[2].Value.Trim()}={editCol.Groups[3].Value.Trim()}", file, cols);

        // EDIT where: عدل حيث col=val → col2=newval
        var editWhere = R(@"^(?:عدل|edit)\s+(?:حيث|where)\s+(.+?)\s*(?:←|->|→|:)\s*(.+)$", RegexOptions.IgnoreCase).Match(msg);
        if (editWhere.Success)
        {
            var cond = editWhere.Groups[1].Value.Trim();
            var vals = editWhere.Groups[2].Value.Trim();
            var (cnt, err) = await CountMatch(file.Id, cond);
            if (err != null) return Fail(err);
            if (cnt == 0) return Ok("لم يُعثر على صفوف تطابق الشرط.");
            return new BotCommandResult
            {
                Reply     = $"⚠️ وُجد **{cnt}** صف يطابق: `{cond}`\nالتعديل: `{vals}`\nهل تريد المتابعة؟ (نعم / لا)",
                NewState  = "edit_confirm",
                StateData = new() { ["condition"] = cond, ["newValues"] = vals },
                Success   = true,
                ActionType = "confirm"
            };
        }

        return Fail("لم أفهم الأمر. اكتب **مساعدة** لمعرفة الأوامر.");
    }

    // ── Multi-turn ───────────────────────────────────────────────────
    private async Task<BotCommandResult> DoAddValues(BotCommandRequest req, UploadedFile file, List<FileColumn> cols, string msg)
    {
        if (IsCancel(msg)) return Cancelled();
        return await ExecAdd(msg, file, cols);
    }

    private async Task<BotCommandResult> DoDeleteConfirm(BotCommandRequest req, UploadedFile file, string msg)
    {
        if (!IsYes(msg)) return Cancelled();
        var d = req.StateData ?? [];

        if (d.GetValueOrDefault("type") == "row")
        {
            int idx = int.Parse(d["rowIndex"]);
            var rec = await db.DataRecords.FirstOrDefaultAsync(r => r.UploadedFileId == file.Id && r.RowIndex == idx);
            if (rec == null) return Fail("الصف غير موجود.");
            db.DataRecords.Remove(rec);
            file.TotalRows = Math.Max(0, file.TotalRows - 1);
            await db.SaveChangesAsync();
            return Done($"✅ تم حذف الصف **{idx}** بنجاح.", "deleted", 1);
        }

        var (recs, err) = await GetMatch(file.Id, d["condition"]);
        if (err != null) return Fail(err);
        db.DataRecords.RemoveRange(recs);
        file.TotalRows = Math.Max(0, file.TotalRows - recs.Count);
        await db.SaveChangesAsync();
        return Done($"✅ تم حذف **{recs.Count}** صف بنجاح.", "deleted", recs.Count);
    }

    private async Task<BotCommandResult> DoEditConfirm(BotCommandRequest req, UploadedFile file, string msg)
    {
        if (!IsYes(msg)) return Cancelled();
        var d = req.StateData ?? [];
        var (recs, err) = await GetMatch(file.Id, d["condition"]);
        if (err != null) return Fail(err);

        var updates = ParseKV(d["newValues"]);
        int changed = 0;
        foreach (var rec in recs)
        {
            var row = Deser(rec.JsonData);
            foreach (var (k, v) in updates)
            {
                var key = row.Keys.FirstOrDefault(rk => Eq(rk, k));
                if (key != null) { row[key] = v; changed++; }
            }
            rec.JsonData = JsonSerializer.Serialize(row);
        }
        await db.SaveChangesAsync();
        return Done($"✅ تم تعديل **{recs.Count}** صف ({changed} قيمة).", "edited", recs.Count);
    }

    // ── Execute ──────────────────────────────────────────────────────
    private async Task<BotCommandResult> ExecAdd(string values, UploadedFile file, List<FileColumn> cols)
    {
        var kv = ParseKV(values);
        if (kv.Count == 0)
            return Fail("صيغة غير صحيحة. مثال: `عمود1=قيمة, عمود2=قيمة`");

        var colNames = cols.Select(c => c.ColumnName).ToList();
        var bad = kv.Keys.Where(k => !colNames.Any(c => Eq(c, k))).ToList();
        if (bad.Any())
            return Fail($"أعمدة غير موجودة: {string.Join(", ", bad)}");

        var row = colNames.ToDictionary(c => c, c =>
            kv.Keys.FirstOrDefault(k => Eq(k, c)) is { } mk ? kv[mk] : "");

        int maxIdx = await db.DataRecords.Where(r => r.UploadedFileId == file.Id)
            .MaxAsync(r => (int?)r.RowIndex) ?? 0;

        var rec = new DataRecord
        {
            UploadedFileId = file.Id,
            RowIndex       = maxIdx + 1,
            JsonData       = JsonSerializer.Serialize(row)
        };
        db.DataRecords.Add(rec);
        file.TotalRows++;
        await db.SaveChangesAsync();

        var preview = string.Join(", ", kv.Take(3).Select(p => $"{p.Key}={p.Value}"));
        return Done($"✅ تم إضافة صف جديد (#{rec.RowIndex}).\n{preview}", "added", 1);
    }

    private async Task<BotCommandResult> ExecEditRow(int idx, string values, UploadedFile file, List<FileColumn> cols)
    {
        var rec = await db.DataRecords.FirstOrDefaultAsync(r => r.UploadedFileId == file.Id && r.RowIndex == idx);
        if (rec == null) return Fail($"الصف {idx} غير موجود.");

        var kv = ParseKV(values);
        if (kv.Count == 0) return Fail("صيغة غير صحيحة. مثال: `عمود=قيمة`");

        var row = Deser(rec.JsonData);
        int changed = 0;
        foreach (var (k, v) in kv)
        {
            var key = row.Keys.FirstOrDefault(rk => Eq(rk, k));
            if (key == null) return Fail($"العمود '{k}' غير موجود.");
            row[key] = v; changed++;
        }
        rec.JsonData = JsonSerializer.Serialize(row);
        await db.SaveChangesAsync();
        return Done($"✅ تم تعديل الصف **{idx}** ({changed} قيمة).", "edited", 1);
    }

    // ── DB helpers ───────────────────────────────────────────────────
    private async Task<(int, string?)> CountMatch(int fileId, string cond)
    {
        var (recs, err) = await GetMatch(fileId, cond);
        return (recs.Count, err);
    }

    private async Task<(List<DataRecord>, string?)> GetMatch(int fileId, string cond)
    {
        var m = R(@"^(.+?)\s*([=!><]+|contains)\s*(.+)$").Match(cond);
        if (!m.Success)
            return ([], "صيغة الشرط غير صحيحة. مثال: `عمود=قيمة`");

        var col = m.Groups[1].Value.Trim();
        var op  = m.Groups[2].Value.Trim();
        var val = m.Groups[3].Value.Trim().Trim('"', '\'');

        var all = await db.DataRecords.Where(r => r.UploadedFileId == fileId).ToListAsync();
        var matched = all.Where(r =>
        {
            var row = Deser(r.JsonData);
            var rv  = row.FirstOrDefault(p => Eq(p.Key, col)).Value ?? "";
            return op switch
            {
                "contains"      => rv.Contains(val, StringComparison.OrdinalIgnoreCase),
                "=" or "=="     => Eq(rv, val),
                "!="            => !Eq(rv, val),
                _ when double.TryParse(rv,  out var d1)
                    && double.TryParse(val, out var d2)
                    => op switch { ">" => d1>d2, "<" => d1<d2, ">=" => d1>=d2, "<=" => d1<=d2, _ => false },
                _ => Eq(rv, val)
            };
        }).ToList();

        return (matched, null);
    }

    // ── Utils ────────────────────────────────────────────────────────
    private static Dictionary<string, string> ParseKV(string input)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in R(@"([^=,\n]+?)\s*=\s*([^,\n]+)").Matches(input))
            d[m.Groups[1].Value.Trim()] = m.Groups[2].Value.Trim();
        return d;
    }

    private static string RowPreview(DataRecord rec, int take)
        => string.Join(", ", Deser(rec.JsonData).Take(take).Select(p => $"{p.Key}={p.Value}"));

    private static Dictionary<string, string> Deser(string json)
        => JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];

    private static bool Eq(string a, string b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static bool IsYes(string s)
        => Regex.IsMatch(s.Trim(), @"^(نعم|yes|أكد|اكد|موافق|y|ok)$", RegexOptions.IgnoreCase);

    private static bool IsCancel(string s)
        => Regex.IsMatch(s.Trim(), @"^(لا|no|الغاء|إلغاء|cancel|n)$", RegexOptions.IgnoreCase);

    private static Regex R(string p, RegexOptions o = RegexOptions.None) => new(p, o);

    private static BotCommandResult Fail(string m) =>
        new() { Reply = $"❌ {m}", NewState = "idle", Success = false, ActionType = "error" };
    private static BotCommandResult Ok(string m) =>
        new() { Reply = $"ℹ️ {m}", NewState = "idle", Success = true, ActionType = "info" };
    private static BotCommandResult Cancelled() =>
        new() { Reply = "↩️ تم الإلغاء.", NewState = "idle", Success = true, ActionType = "info" };
    private static BotCommandResult Done(string m, string type, int n) =>
        new() { Reply = m, NewState = "idle", Success = true, ActionType = type, AffectedRows = n, RefreshData = true };

    private static BotCommandResult HelpMsg(List<string> cols) => new()
    {
        Reply = $"""
        📖 **الأوامر المتاحة:**

        ➕ إضافة صف:
        أضف: عمود1=قيمة, عمود2=قيمة

        ✏️ تعديل صف بالرقم:
        عدل الصف 5: عمود=قيمة_جديدة

        ✏️ تعديل بشرط:
        عدل حيث عمود=قيمة → عمود2=قيمة_جديدة

        🗑️ حذف صف:
        احذف الصف 5

        🗑️ حذف بشرط:
        احذف حيث عمود=قيمة

        📋 عرض الأعمدة: الأعمدة

        مثال على عمود: {cols.FirstOrDefault() ?? "ColName"}
        """,
        NewState = "idle", Success = true, ActionType = "info"
    };

    private static BotCommandResult ColsMsg(List<string> cols) => new()
    {
        Reply    = $"📋 أعمدة الملف ({cols.Count}):\n" + string.Join("\n", cols.Select((c, i) => $"{i+1}. {c}")),
        NewState = "idle", Success = true, ActionType = "info"
    };
}
