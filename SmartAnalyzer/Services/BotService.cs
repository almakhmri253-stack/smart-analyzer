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

    // ══════════════════════════════════════════════════════════════
    //  PARSER
    // ══════════════════════════════════════════════════════════════
    private async Task<BotCommandResult> ParseCommand(string msg, UploadedFile file, List<FileColumn> cols)
    {
        var lower    = msg.ToLower().Trim();
        var colNames = cols.Select(c => c.ColumnName).ToList();

        // ── Help ──────────────────────────────────────────────────
        if (R(@"^(مساعدة|مساعده|help|\?)").IsMatch(lower))
            return HelpMsg(cols);

        // ── Columns list ──────────────────────────────────────────
        if (R(@"^(الاعمدة|الأعمدة|اعمده|اعمدة|columns?|cols?)$").IsMatch(lower))
            return ColsMsg(cols);

        // ═══════════════════════  READ / SEARCH  ═════════════════════════

        // اعرض الصف 5
        var showRow = R(@"^(?:اعرض|عرض|show|اظهر|عرض الصف)\s+(?:الصف|صف|row)\s*#?\s*(\d+)", RO).Match(msg);
        if (showRow.Success)
            return await ExecShowRow(int.Parse(showRow.Groups[1].Value), file, cols);

        // اعرض أول 10
        var showFirst = R(@"^(?:اعرض|عرض|show)\s+(?:أول|اول|first)\s+(\d+)", RO).Match(msg);
        if (showFirst.Success)
            return await ExecShowTop(int.Parse(showFirst.Groups[1].Value), file, cols, false);

        // اعرض آخر 10
        var showLast = R(@"^(?:اعرض|عرض|show)\s+(?:آخر|اخر|last)\s+(\d+)", RO).Match(msg);
        if (showLast.Success)
            return await ExecShowTop(int.Parse(showLast.Groups[1].Value), file, cols, true);

        // اعرض حيث / ابحث حيث
        var showWhere = R(@"^(?:اعرض|عرض|show|ابحث|بحث|search|find)\s+(?:حيث|where|إذا|اذا)\s+(.+)$", RO).Match(msg);
        if (showWhere.Success)
            return await ExecShowWhere(showWhere.Groups[1].Value.Trim(), file, cols);

        // ابحث في col عن val
        var searchIn = R(@"^(?:ابحث|بحث|search)\s+(?:في|in)?\s*(.+?)\s+(?:عن|about|=)\s*(.+)$", RO).Match(msg);
        if (searchIn.Success)
            return await ExecShowWhere($"{searchIn.Groups[1].Value.Trim()}={searchIn.Groups[2].Value.Trim()}", file, cols);

        // ابحث قيمة (global search)
        var searchGlobal = R(@"^(?:ابحث|بحث|search)\s+(.+)$", RO).Match(msg);
        if (searchGlobal.Success && !searchGlobal.Groups[1].Value.Contains('='))
            return await ExecSearchGlobal(searchGlobal.Groups[1].Value.Trim(), file, cols);

        // ═══════════════════════  STATISTICS  ════════════════════════════

        // عدد الصفوف
        if (R(@"^(?:عدد|عد|count)\s+(?:الصفوف|صفوف|rows?|all|الكل|الجميع)$", RO).IsMatch(msg))
            return await ExecCount(null, file);

        // عدد حيث
        var countWhere = R(@"^(?:عدد|عد|count)\s+(?:حيث|where)\s+(.+)$", RO).Match(msg);
        if (countWhere.Success)
            return await ExecCount(countWhere.Groups[1].Value.Trim(), file);

        // مجموع / جمع
        var sumWhere = R(@"^(?:مجموع|جمع|sum)\s+(.+?)\s+(?:حيث|where|إذا)\s+(.+)$", RO).Match(msg);
        if (sumWhere.Success)
            return await ExecAggregate("sum", sumWhere.Groups[1].Value.Trim(), sumWhere.Groups[2].Value.Trim(), file);
        var sumCol = R(@"^(?:مجموع|جمع|sum)\s+(.+)$", RO).Match(msg);
        if (sumCol.Success)
            return await ExecAggregate("sum", sumCol.Groups[1].Value.Trim(), null, file);

        // متوسط / معدل
        var avgWhere = R(@"^(?:متوسط|معدل|avg|average)\s+(.+?)\s+(?:حيث|where|إذا)\s+(.+)$", RO).Match(msg);
        if (avgWhere.Success)
            return await ExecAggregate("avg", avgWhere.Groups[1].Value.Trim(), avgWhere.Groups[2].Value.Trim(), file);
        var avgCol = R(@"^(?:متوسط|معدل|avg|average)\s+(.+)$", RO).Match(msg);
        if (avgCol.Success)
            return await ExecAggregate("avg", avgCol.Groups[1].Value.Trim(), null, file);

        // أكبر / أعلى / max
        var maxWhere = R(@"^(?:أكبر|اكبر|أعلى|اعلى|max|maximum)\s+(.+?)\s+(?:حيث|where|إذا)\s+(.+)$", RO).Match(msg);
        if (maxWhere.Success)
            return await ExecAggregate("max", maxWhere.Groups[1].Value.Trim(), maxWhere.Groups[2].Value.Trim(), file);
        var maxCol = R(@"^(?:أكبر|اكبر|أعلى|اعلى|max|maximum)\s+(.+)$", RO).Match(msg);
        if (maxCol.Success)
            return await ExecAggregate("max", maxCol.Groups[1].Value.Trim(), null, file);

        // أصغر / أدنى / min
        var minWhere = R(@"^(?:أصغر|اصغر|أدنى|ادنى|min|minimum)\s+(.+?)\s+(?:حيث|where|إذا)\s+(.+)$", RO).Match(msg);
        if (minWhere.Success)
            return await ExecAggregate("min", minWhere.Groups[1].Value.Trim(), minWhere.Groups[2].Value.Trim(), file);
        var minCol = R(@"^(?:أصغر|اصغر|أدنى|ادنى|min|minimum)\s+(.+)$", RO).Match(msg);
        if (minCol.Success)
            return await ExecAggregate("min", minCol.Groups[1].Value.Trim(), null, file);

        // إحصاء شامل لعمود
        var statCol = R(@"^(?:إحصاء|احصاء|stats?|statistics)\s+(.+)$", RO).Match(msg);
        if (statCol.Success)
            return await ExecStats(statCol.Groups[1].Value.Trim(), file);

        // ═══════════════════════  ADD  ════════════════════════════════════

        // أضف inline: أضف: col=val
        var addInline = R(@"^(?:اضف|أضف|add)\s*[:\-]\s*(.+)", RO).Match(msg);
        if (addInline.Success && addInline.Groups[1].Value.Contains('='))
            return await ExecAdd(addInline.Groups[1].Value, file, cols);

        // أضف → ask
        if (R(@"^(?:اضف|أضف|add)\b", RO).IsMatch(lower))
        {
            var sample = string.Join(", ", colNames.Take(4).Select(c => $"{c}=قيمة"));
            return new BotCommandResult
            {
                Reply      = $"📝 أدخل قيم الصف الجديد بالصيغة:\n`{sample}`",
                NewState   = "add_waiting",
                StateData  = [],
                Success    = true,
                ActionType = "prompt"
            };
        }

        // ═══════════════════════  DELETE  ════════════════════════════════

        // احذف الصف N
        var delRow = R(@"^(?:احذف|حذف|delete|del)\s+(?:الصف|صف|row)\s*#?\s*(\d+)", RO).Match(msg);
        if (delRow.Success)
        {
            int idx = int.Parse(delRow.Groups[1].Value);
            var rec = await db.DataRecords.FirstOrDefaultAsync(r => r.UploadedFileId == file.Id && r.RowIndex == idx);
            if (rec == null) return Fail($"الصف **{idx}** غير موجود.");
            return new BotCommandResult
            {
                Reply      = $"⚠️ تأكيد حذف الصف **#{idx}**:\n\n{RowPreview(rec, cols, 5)}\n\n─────────────\nاكتب **نعم** لتأكيد الحذف أو **لا** للإلغاء",
                NewState   = "delete_confirm",
                StateData  = new() { ["type"] = "row", ["rowIndex"] = idx.ToString() },
                Success    = true,
                ActionType = "confirm"
            };
        }

        // احذف حيث col=val
        var delWhere = R(@"^(?:احذف|حذف|delete|del)\s+(?:حيث|where|إذا)\s+(.+)$", RO).Match(msg);
        if (delWhere.Success)
        {
            var cond = delWhere.Groups[1].Value.Trim();
            var (cnt, err) = await CountMatch(file.Id, cond);
            if (err != null) return Fail(err);
            if (cnt == 0) return Info("لم يُعثر على صفوف تطابق الشرط.");
            return new BotCommandResult
            {
                Reply      = $"⚠️ وُجد **{cnt}** صف يطابق: `{cond}`\n\nهل تريد حذفها جميعاً؟\nاكتب **نعم** للتأكيد أو **لا** للإلغاء",
                NewState   = "delete_confirm",
                StateData  = new() { ["type"] = "condition", ["condition"] = cond },
                Success    = true,
                ActionType = "confirm"
            };
        }

        // ═══════════════════════  EDIT  ══════════════════════════════════

        // عدل الصف N: col=val
        var editRow = R(@"^(?:عدل|تعديل|edit|غير|بدل)\s+(?:الصف|صف|row)\s*#?\s*(\d+)\s*[:\-]\s*(.+)$", RO).Match(msg);
        if (editRow.Success)
            return await ExecEditRow(int.Parse(editRow.Groups[1].Value), editRow.Groups[2].Value, file, cols);

        // عدل الصف N عمود X إلى Y
        var editTo = R(@"^(?:عدل|تعديل|edit|غير|بدل)\s+(?:الصف|صف|row)\s*#?\s*(\d+)\s+(?:عمود|col(?:umn)?)?\s*(.+?)\s+(?:إلى|الى|to|=)\s*(.+)$", RO).Match(msg);
        if (editTo.Success)
            return await ExecEditRow(int.Parse(editTo.Groups[1].Value),
                $"{editTo.Groups[2].Value.Trim()}={editTo.Groups[3].Value.Trim()}", file, cols);

        // عدل حيث col=val → col2=newval
        var editWhere = R(@"^(?:عدل|تعديل|edit|غير|بدل)\s+(?:حيث|where|إذا)\s+(.+?)\s*(?:←|->|→|:)\s*(.+)$", RO).Match(msg);
        if (editWhere.Success)
        {
            var cond = editWhere.Groups[1].Value.Trim();
            var vals = editWhere.Groups[2].Value.Trim();
            var (cnt, err) = await CountMatch(file.Id, cond);
            if (err != null) return Fail(err);
            if (cnt == 0) return Info("لم يُعثر على صفوف تطابق الشرط.");
            return new BotCommandResult
            {
                Reply      = $"⚠️ وُجد **{cnt}** صف يطابق: `{cond}`\nسيتم تطبيق: `{vals}`\n\nاكتب **نعم** للتأكيد أو **لا** للإلغاء",
                NewState   = "edit_confirm",
                StateData  = new() { ["condition"] = cond, ["newValues"] = vals },
                Success    = true,
                ActionType = "confirm"
            };
        }

        return Fail("لم أفهم الأمر.\nاكتب **مساعدة** لعرض جميع الأوامر المتاحة.");
    }

    // ══════════════════════════════════════════════════════════════
    //  MULTI-TURN
    // ══════════════════════════════════════════════════════════════
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
            return Done($"✅ تم حذف الصف **#{idx}** بنجاح.", "deleted", 1);
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
        var d    = req.StateData ?? [];
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

    // ══════════════════════════════════════════════════════════════
    //  EXECUTE COMMANDS
    // ══════════════════════════════════════════════════════════════
    private async Task<BotCommandResult> ExecAdd(string values, UploadedFile file, List<FileColumn> cols)
    {
        var kv       = ParseKV(values);
        if (kv.Count == 0) return Fail("صيغة غير صحيحة.\nمثال: `عمود1=قيمة, عمود2=قيمة`");

        var colNames = cols.Select(c => c.ColumnName).ToList();
        var bad      = kv.Keys.Where(k => !colNames.Any(c => Eq(c, k))).ToList();
        if (bad.Any())
            return Fail($"أعمدة غير موجودة: **{string.Join(", ", bad)}**\nاكتب **الأعمدة** لعرض الأعمدة المتاحة.");

        var row = colNames.ToDictionary(c => c, c =>
            kv.Keys.FirstOrDefault(k => Eq(k, c)) is { } mk ? kv[mk] : "");

        int maxIdx = await db.DataRecords
            .Where(r => r.UploadedFileId == file.Id)
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

        var preview = string.Join("\n", kv.Select(p => $"• **{p.Key}**: {p.Value}"));
        return Done($"✅ تمت الإضافة بنجاح — الصف **#{rec.RowIndex}**\n\n{preview}", "added", 1);
    }

    private async Task<BotCommandResult> ExecEditRow(int idx, string values, UploadedFile file, List<FileColumn> cols)
    {
        var rec = await db.DataRecords.FirstOrDefaultAsync(r => r.UploadedFileId == file.Id && r.RowIndex == idx);
        if (rec == null) return Fail($"الصف **{idx}** غير موجود.");

        var kv = ParseKV(values);
        if (kv.Count == 0) return Fail("صيغة غير صحيحة.\nمثال: `عمود=قيمة`");

        var row     = Deser(rec.JsonData);
        var changes = new List<string>();
        foreach (var (k, v) in kv)
        {
            var key = row.Keys.FirstOrDefault(rk => Eq(rk, k));
            if (key == null) return Fail($"العمود '**{k}**' غير موجود.\nاكتب **الأعمدة** للاطلاع على الأعمدة المتاحة.");
            changes.Add($"• **{key}**: {row[key]} → **{v}**");
            row[key] = v;
        }
        rec.JsonData = JsonSerializer.Serialize(row);
        await db.SaveChangesAsync();
        return Done($"✅ تم تعديل الصف **#{idx}** ({changes.Count} تغيير)\n\n{string.Join("\n", changes)}", "edited", 1);
    }

    private async Task<BotCommandResult> ExecShowRow(int idx, UploadedFile file, List<FileColumn> cols)
    {
        var rec = await db.DataRecords.FirstOrDefaultAsync(r => r.UploadedFileId == file.Id && r.RowIndex == idx);
        if (rec == null) return Fail($"الصف **{idx}** غير موجود.");
        return BuildTableResult([rec], cols, $"الصف #{idx}");
    }

    private async Task<BotCommandResult> ExecShowTop(int n, UploadedFile file, List<FileColumn> cols, bool last)
    {
        n = Math.Clamp(n, 1, 100);
        List<DataRecord> recs;
        if (last)
            recs = await db.DataRecords.Where(r => r.UploadedFileId == file.Id)
                .OrderByDescending(r => r.RowIndex).Take(n).OrderBy(r => r.RowIndex).ToListAsync();
        else
            recs = await db.DataRecords.Where(r => r.UploadedFileId == file.Id)
                .OrderBy(r => r.RowIndex).Take(n).ToListAsync();

        return BuildTableResult(recs, cols, last ? $"آخر {n} صفوف" : $"أول {n} صفوف");
    }

    private async Task<BotCommandResult> ExecShowWhere(string cond, UploadedFile file, List<FileColumn> cols)
    {
        var (recs, err) = await GetMatch(file.Id, cond);
        if (err != null) return Fail(err);
        if (recs.Count == 0) return Info("لم يُعثر على صفوف تطابق الشرط.");
        var shown = recs.Take(50).ToList();
        var title = recs.Count > 50
            ? $"نتائج البحث — أول 50 من {recs.Count} صف"
            : $"نتائج البحث — {recs.Count} صف";
        return BuildTableResult(shown, cols, title);
    }

    private async Task<BotCommandResult> ExecSearchGlobal(string term, UploadedFile file, List<FileColumn> cols)
    {
        var all = await db.DataRecords.Where(r => r.UploadedFileId == file.Id).ToListAsync();
        var matched = all.Where(r =>
            Deser(r.JsonData).Values.Any(v => v != null && v.Contains(term, StringComparison.OrdinalIgnoreCase))
        ).Take(50).ToList();

        if (!matched.Any()) return Info($"لم يُعثر على نتائج تحتوي على \"{term}\".");
        return BuildTableResult(matched, cols, $"بحث عن \"{term}\" — {matched.Count} نتيجة");
    }

    private async Task<BotCommandResult> ExecCount(string? cond, UploadedFile file)
    {
        if (cond == null)
        {
            var total = await db.DataRecords.CountAsync(r => r.UploadedFileId == file.Id);
            return Stat($"إجمالي الصفوف", total.ToString("N0"), "count");
        }
        var (cnt, err) = await CountMatch(file.Id, cond);
        if (err != null) return Fail(err);
        return Stat($"عدد الصفوف حيث `{cond}`", cnt.ToString("N0"), "count");
    }

    private async Task<BotCommandResult> ExecAggregate(string op, string colName, string? cond, UploadedFile file)
    {
        List<DataRecord> recs;
        if (cond != null)
        {
            var (matched, err) = await GetMatch(file.Id, cond);
            if (err != null) return Fail(err);
            recs = matched;
        }
        else
        {
            recs = await db.DataRecords.Where(r => r.UploadedFileId == file.Id).ToListAsync();
        }

        var vals = recs
            .Select(r => Deser(r.JsonData).FirstOrDefault(p => Eq(p.Key, colName)).Value)
            .Where(v => v != null && double.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
            .Select(v => double.Parse(v!, System.Globalization.CultureInfo.InvariantCulture))
            .ToList();

        if (!vals.Any())
            return Fail($"لم يُعثر على قيم رقمية في العمود '**{colName}**'.");

        var condSuffix = cond != null ? $" (حيث {cond})" : "";
        var (label, value, icon) = op switch
        {
            "sum" => ($"مجموع {colName}{condSuffix}", vals.Sum().ToString("N2"), "sum"),
            "avg" => ($"متوسط {colName}{condSuffix}", vals.Average().ToString("N2"), "avg"),
            "max" => ($"أكبر قيمة في {colName}{condSuffix}", vals.Max().ToString("N2"), "max"),
            "min" => ($"أصغر قيمة في {colName}{condSuffix}", vals.Min().ToString("N2"), "min"),
            _     => ("", "", "")
        };
        var sub = $"من {vals.Count:N0} صف";
        return StatFull(label, value, sub, icon);
    }

    private async Task<BotCommandResult> ExecStats(string colName, UploadedFile file)
    {
        var recs = await db.DataRecords.Where(r => r.UploadedFileId == file.Id).ToListAsync();
        var vals = recs
            .Select(r => Deser(r.JsonData).FirstOrDefault(p => Eq(p.Key, colName)).Value)
            .Where(v => v != null && double.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
            .Select(v => double.Parse(v!, System.Globalization.CultureInfo.InvariantCulture))
            .ToList();

        if (!vals.Any())
            return Fail($"لم يُعثر على قيم رقمية في العمود '**{colName}**'.");

        var reply = $"""
        📊 **إحصاء عمود: {colName}**

        • العدد: **{vals.Count:N0}**
        • المجموع: **{vals.Sum():N2}**
        • المتوسط: **{vals.Average():N2}**
        • أكبر قيمة: **{vals.Max():N2}**
        • أصغر قيمة: **{vals.Min():N2}**
        • الانحراف المعياري: **{StdDev(vals):N2}**
        """;
        return Info2(reply);
    }

    // ══════════════════════════════════════════════════════════════
    //  HELPERS — DB
    // ══════════════════════════════════════════════════════════════
    private async Task<(int, string?)> CountMatch(int fileId, string cond)
    {
        var (recs, err) = await GetMatch(fileId, cond);
        return (recs.Count, err);
    }

    private async Task<(List<DataRecord>, string?)> GetMatch(int fileId, string cond)
    {
        var m = R(@"^(.+?)\s*([=!><]+|contains)\s*(.+)$").Match(cond);
        if (!m.Success)
            return ([], "صيغة الشرط غير صحيحة.\nمثال: `عمود=قيمة` أو `عمود>100`");

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
                "contains"  => rv.Contains(val, StringComparison.OrdinalIgnoreCase),
                "=" or "==" => Eq(rv, val),
                "!="        => !Eq(rv, val),
                _ when double.TryParse(rv,  System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d1)
                    && double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d2)
                    => op switch { ">" => d1>d2, "<" => d1<d2, ">=" => d1>=d2, "<=" => d1<=d2, _ => false },
                _ => Eq(rv, val)
            };
        }).ToList();

        return (matched, null);
    }

    // ══════════════════════════════════════════════════════════════
    //  HELPERS — FORMATTING
    // ══════════════════════════════════════════════════════════════
    private static BotCommandResult BuildTableResult(List<DataRecord> recs, List<FileColumn> cols, string title)
    {
        if (!recs.Any()) return Info("لا توجد صفوف.");

        var colNames = cols.Select(c => c.ColumnName).ToList();
        var rows = recs.Select(r =>
        {
            var data = Deser(r.JsonData);
            return new BotTableRow
            {
                RowIndex = r.RowIndex,
                Data     = data
            };
        }).ToList();

        return new BotCommandResult
        {
            Reply      = title,
            NewState   = "idle",
            Success    = true,
            ActionType = "table",
            TableData  = new BotTableData { Title = title, Columns = colNames, Rows = rows }
        };
    }

    private static string RowPreview(DataRecord rec, List<FileColumn> cols, int take)
    {
        var row = Deser(rec.JsonData);
        return string.Join("\n", row.Take(take).Select(p => $"• **{p.Key}**: {(string.IsNullOrEmpty(p.Value) ? "—" : p.Value)}"));
    }

    private static double StdDev(List<double> vals)
    {
        if (vals.Count < 2) return 0;
        var avg = vals.Average();
        return Math.Sqrt(vals.Average(v => Math.Pow(v - avg, 2)));
    }

    private static Dictionary<string, string> ParseKV(string input)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in R(@"([^=,\n]+?)\s*=\s*([^,\n]+)").Matches(input))
            d[m.Groups[1].Value.Trim()] = m.Groups[2].Value.Trim();
        return d;
    }

    private static Dictionary<string, string> Deser(string json)
        => JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];

    private static bool Eq(string a, string b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static bool IsYes(string s)
        => Regex.IsMatch(s.Trim(), @"^(نعم|yes|أكد|اكد|موافق|y|ok|تأكيد|تاكيد|ن)$", RegexOptions.IgnoreCase);

    private static bool IsCancel(string s)
        => Regex.IsMatch(s.Trim(), @"^(لا|no|إلغاء|الغاء|cancel|n|إلغ|الغ)$", RegexOptions.IgnoreCase);

    private static Regex R(string p, RegexOptions o = RegexOptions.None) => new(p, o);
    private static readonly RegexOptions RO = RegexOptions.IgnoreCase;

    // ══════════════════════════════════════════════════════════════
    //  RESULT BUILDERS
    // ══════════════════════════════════════════════════════════════
    private static BotCommandResult Fail(string m) =>
        new() { Reply = m, NewState = "idle", Success = false, ActionType = "error" };

    private static BotCommandResult Info(string m) =>
        new() { Reply = m, NewState = "idle", Success = true, ActionType = "info" };

    private static BotCommandResult Info2(string m) =>
        new() { Reply = m, NewState = "idle", Success = true, ActionType = "stats" };

    private static BotCommandResult Stat(string label, string value, string icon) =>
        new() { Reply = $"{label}||{value}||{icon}", NewState = "idle", Success = true, ActionType = "stat" };

    private static BotCommandResult StatFull(string label, string value, string sub, string icon) =>
        new() { Reply = $"{label}||{value}||{sub}||{icon}", NewState = "idle", Success = true, ActionType = "stat" };

    private static BotCommandResult Cancelled() =>
        new() { Reply = "↩️ تم الإلغاء.", NewState = "idle", Success = true, ActionType = "info" };

    private static BotCommandResult Done(string m, string type, int n) =>
        new() { Reply = m, NewState = "idle", Success = true, ActionType = type, AffectedRows = n, RefreshData = true };

    // ══════════════════════════════════════════════════════════════
    //  HELP & COLUMNS
    // ══════════════════════════════════════════════════════════════
    private static BotCommandResult HelpMsg(List<FileColumn> cols)
    {
        var c1 = cols.FirstOrDefault()?.ColumnName ?? "العمود";
        var c2 = cols.Skip(1).FirstOrDefault()?.ColumnName ?? "العمود2";
        return new()
        {
            Reply = $"""
            📖 **دليل الأوامر الكاملة**

            **🔍 القراءة والبحث:**
            اعرض الصف 5
            اعرض أول 10 · اعرض آخر 5
            اعرض حيث {c1}=قيمة
            ابحث في {c1} عن قيمة
            ابحث كلمة

            **📊 الإحصاء والحسابات:**
            عدد الصفوف
            عدد حيث {c1}=قيمة
            مجموع {c1}
            متوسط {c1}
            أكبر {c1} · أصغر {c1}
            مجموع {c1} حيث {c2}=قيمة
            إحصاء {c1}

            **➕ الإضافة:**
            أضف: {c1}=قيمة, {c2}=قيمة

            **✏️ التعديل:**
            عدل الصف 5: {c1}=جديد
            عدل الصف 5 عمود {c1} إلى جديد
            عدل حيث {c1}=قديم → {c1}=جديد

            **🗑️ الحذف:**
            احذف الصف 5
            احذف حيث {c1}=قيمة

            **📋 الأعمدة** — عرض الأعمدة
            """,
            NewState = "idle", Success = true, ActionType = "info"
        };
    }

    private static BotCommandResult ColsMsg(List<FileColumn> cols) => new()
    {
        Reply    = $"📋 **أعمدة الملف** ({cols.Count} عمود):\n\n" +
                   string.Join("\n", cols.Select((c, i) => $"`{i+1}.` **{c.ColumnName}**  _{c.ColumnType}_")),
        NewState = "idle", Success = true, ActionType = "info"
    };
}
