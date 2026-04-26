const TRANSLATIONS = {
    ar: {
        // App
        'app.title': 'المحلل الذكي',
        // Sidebar
        'nav.dashboard': 'لوحة التحكم',
        'nav.upload': 'رفع ملف Excel',
        'nav.analysis': 'التحليل والمقارنات',
        'nav.users': 'إدارة المستخدمين',
        'nav.logout': 'تسجيل الخروج',
        'topbar.home': 'الرئيسية',
        // Upload modal
        'upload.title': 'رفع ملف Excel',
        'upload.drop': 'اسحب وأفلت ملف Excel هنا أو',
        'upload.choose': 'اختر ملفاً',
        'upload.no_file': 'لم يتم اختيار ملف',
        'upload.no_limit': 'لا يوجد حد أقصى لحجم الملف',
        'upload.sheet': 'الشيت:',
        'upload.click_row': 'اضغط على صف الرؤوس',
        'upload.cancel': 'إلغاء',
        'upload.preview_btn': 'معاينة واختر الصف',
        'upload.upload_btn': 'رفع الملف وتحليله',
        'upload.loading': 'جاري قراءة الملف...',
        'upload.change_file': 'تغيير الملف',
        // Analysis – static
        'a.back': 'رجوع',
        'a.row': 'صف',
        'a.col': 'عمود',
        'a.all_data': 'كل البيانات',
        'a.quality': 'جودة البيانات:',
        'a.save_filter': 'حفظ الفلتر',
        'a.filters_btn': 'الفلاتر',
        'a.export': 'تصدير',
        'a.filter_name_ph': 'اسم الفلتر...',
        'a.saved_filters': 'الفلاتر المحفوظة',
        'a.col_label': 'عمود:',
        'a.kpi_count': 'إجمالي عدد العقود',
        'a.kpi_value': 'إجمالي قيمة العقود',
        'a.kpi_sub_value': 'Total Contract Value (USD)',
        'a.compliance_title': 'Compliance of Omanization',
        'a.conditions_title': 'الشروط والفلاتر',
        'a.new_cond': 'شرط جديد',
        'a.apply': 'تطبيق',
        'a.reset': 'إعادة تعيين',
        'a.data_title': 'البيانات',
        'a.search_ph': 'بحث في الجدول...',
        'a.loading': 'جاري التحليل...',
        'a.vs_label': 'مقابل',
        // Analysis – JS dynamic
        'j.no_results': 'لا توجد نتائج',
        'j.loading': 'جاري...',
        'j.no_data': 'لا توجد بيانات',
        'j.all': 'الكل',
        'j.records': 'سجل',
        'j.filtered_from': 'سجل مصفّى من',
        'j.total_records': 'إجمالي السجلات',
        'j.showing': 'عرض',
        'j.of': 'من',
        'j.filter_in_table': 'تصفية في الجدول',
        'j.close': 'إغلاق',
        'j.col_placeholder': '-- العمود --',
        'j.value_ph': 'القيمة',
        'j.to_ph': 'إلى...',
        'j.loading_saved': 'جاري التحميل...',
        'j.no_saved': 'لا توجد فلاتر محفوظة.',
        'j.delete_confirm': 'حذف هذا الفلتر؟',
        'j.filter_saved': 'تم حفظ الفلتر ✓',
        'j.filter_deleted': 'تم الحذف',
        'j.enter_name': 'أدخل اسماً للفلتر',
        'j.total_records_of': 'سجل | {cols} عمود',
        'j.analyzing': 'جاري التحليل...',
        // Chart export
        'chart.export_png': 'تصدير PNG',
        'chart.export_all': 'تصدير جميع المخططات',
        // Upload modal JS
        'upload.file_info': 'الملف كاملاً: {rows} صف × {cols} عمود',
        'upload.preview_limit': 'المعاينة: أول {n} صف فقط — الرفع يشمل الملف كاملاً',
        'upload.row_selected': 'الصف {n} كرأس الأعمدة — الأعمدة: {cols}',
        'upload.load_fail': 'فشل تحميل المعاينة',
        // Dashboard static
        'dsh.files_count': 'ملفات مرفوعة',
        'dsh.total_records': 'إجمالي السجلات',
        'dsh.current_cols': 'أعمدة الملف الحالي',
        'dsh.saved_filters_lbl': 'فلاتر محفوظة',
        'dsh.my_files': 'ملفاتي',
        'dsh.no_files': 'لا توجد ملفات. ارفع ملفك الأول!',
        'dsh.record': 'سجل',
        'dsh.analyze_file': 'تحليل الملف',
        'dsh.delete_file_confirm': 'هل تريد حذف هذا الملف؟',
        'dsh.saved_filters_title': 'الفلاتر المحفوظة',
        'dsh.choose_file': 'اختر ملفاً لبدء التحليل',
        'dsh.or_upload': 'أو ارفع ملف Excel جديد',
        'dsh.upload_excel': 'رفع ملف Excel',
        'dsh.filter_builder': 'منشئ الفلاتر',
        'dsh.logic_lbl': 'المنطق:',
        'dsh.add_condition': 'إضافة شرط',
        'dsh.apply_filter': 'تطبيق الفلتر',
        'dsh.reset_filters': 'إعادة تعيين',
        'dsh.save_filter': 'حفظ الفلتر',
        'dsh.export_excel': 'تصدير Excel',
        'dsh.filter_name_ph': 'اسم الفلتر...',
        'dsh.save': 'حفظ',
        'dsh.cancel': 'إلغاء',
        'dsh.summary_title': 'ملخص النتائج',
        'dsh.chart_title': 'الرسم البياني',
        'dsh.distribution': 'التوزيع',
        'dsh.data_title': 'البيانات',
        'dsh.search_ph': 'بحث في الجدول...',
        'dsh.preview_info': 'عرض أول 5 سجلات (معاينة)',
        'dsh.col_placeholder': '-- اختر العمود --',
        'dsh.no_results': 'لا توجد نتائج تطابق الشروط',
        'dsh.total_results': 'إجمالي النتائج',
        'dsh.sum': 'المجموع',
        'dsh.avg': 'المتوسط',
        'dsh.min': 'الأدنى',
        'dsh.max': 'الأعلى',
        'dsh.showing': 'عرض',
        'dsh.of': 'من',
        'dsh.enter_filter_name': 'الرجاء إدخال اسم للفلتر',
        'dsh.filter_saved_ok': 'تم حفظ الفلتر بنجاح!',
        // User Management
        'um.title': 'إدارة المستخدمين',
        'um.subtitle': 'إضافة وتعديل وحذف المستخدمين وتحديد صلاحياتهم',
        'um.add_user': 'إضافة مستخدم',
        'um.col_name': 'الاسم',
        'um.col_email': 'البريد الإلكتروني',
        'um.col_role': 'الصلاحية',
        'um.col_date': 'تاريخ الإنشاء',
        'um.col_actions': 'الإجراءات',
        'um.you': 'أنت',
        'um.role_admin': 'مدير',
        'um.role_user': 'مستخدم',
        'um.total': 'إجمالي المستخدمين',
        'um.no_users': 'لا يوجد مستخدمون',
        'um.create_title': 'إضافة مستخدم جديد',
        'um.fullname': 'الاسم الكامل',
        'um.fullname_ph': 'أدخل الاسم الكامل',
        'um.password': 'كلمة المرور',
        'um.pwd_ph': '6 أحرف على الأقل',
        'um.cancel': 'إلغاء',
        'um.create_btn': 'إنشاء',
        'um.edit_title': 'تعديل المستخدم',
        'um.new_pwd': 'كلمة مرور جديدة',
        'um.new_pwd_ph': 'اتركه فارغاً للإبقاء على الحالية',
        'um.optional': 'اختياري',
        'um.save': 'حفظ',
        'um.delete_title': 'حذف المستخدم',
        'um.delete_confirm': 'هل أنت متأكد من حذف',
        'um.delete_warning': 'لا يمكن التراجع عن هذا الإجراء.',
        'um.delete_btn': 'حذف',
    },
    en: {
        // App
        'app.title': 'Smart Analyzer',
        // Sidebar
        'nav.dashboard': 'Dashboard',
        'nav.upload': 'Upload Excel File',
        'nav.analysis': 'Analysis & Comparisons',
        'nav.users': 'User Management',
        'nav.logout': 'Sign Out',
        'topbar.home': 'Home',
        // Upload modal
        'upload.title': 'Upload Excel File',
        'upload.drop': 'Drag & drop an Excel file here or',
        'upload.choose': 'Choose File',
        'upload.no_file': 'No file selected',
        'upload.no_limit': 'No file size limit',
        'upload.sheet': 'Sheet:',
        'upload.click_row': 'Click on the header row',
        'upload.cancel': 'Cancel',
        'upload.preview_btn': 'Preview & Select Row',
        'upload.upload_btn': 'Upload & Analyze',
        'upload.loading': 'Reading file...',
        'upload.change_file': 'Change File',
        // Analysis – static
        'a.back': 'Back',
        'a.row': 'rows',
        'a.col': 'columns',
        'a.all_data': 'All Data',
        'a.quality': 'Data Quality:',
        'a.save_filter': 'Save Filter',
        'a.filters_btn': 'Filters',
        'a.export': 'Export',
        'a.filter_name_ph': 'Filter name...',
        'a.saved_filters': 'Saved Filters',
        'a.col_label': 'Column:',
        'a.kpi_count': 'Total Contracts',
        'a.kpi_value': 'Total Contract Value',
        'a.kpi_sub_value': 'Total Contract Value (USD)',
        'a.compliance_title': 'Compliance of Omanization',
        'a.conditions_title': 'Filters & Conditions',
        'a.new_cond': 'New Condition',
        'a.apply': 'Apply',
        'a.reset': 'Reset',
        'a.data_title': 'Data',
        'a.search_ph': 'Search in table...',
        'a.loading': 'Analyzing...',
        'a.vs_label': 'vs',
        // Analysis – JS dynamic
        'j.no_results': 'No results found',
        'j.loading': 'Loading...',
        'j.no_data': 'No data available',
        'j.all': 'All',
        'j.records': 'records',
        'j.filtered_from': 'filtered from',
        'j.total_records': 'Total Records',
        'j.showing': 'Showing',
        'j.of': 'of',
        'j.filter_in_table': 'Filter in Table',
        'j.close': 'Close',
        'j.col_placeholder': '-- Column --',
        'j.value_ph': 'Value',
        'j.to_ph': 'To...',
        'j.loading_saved': 'Loading...',
        'j.no_saved': 'No saved filters.',
        'j.delete_confirm': 'Delete this filter?',
        'j.filter_saved': 'Filter saved ✓',
        'j.filter_deleted': 'Deleted',
        'j.enter_name': 'Enter a filter name',
        'j.total_records_of': 'records | {cols} columns',
        'j.analyzing': 'Analyzing...',
        // Chart export
        'chart.export_png': 'Export PNG',
        'chart.export_all': 'Export All Charts',
        // Upload modal JS
        'upload.file_info': 'Full file: {rows} rows × {cols} columns',
        'upload.preview_limit': 'Preview: first {n} rows only — upload includes full file',
        'upload.row_selected': 'Row {n} as header — Columns: {cols}',
        'upload.load_fail': 'Failed to load preview',
        // Dashboard static
        'dsh.files_count': 'Uploaded Files',
        'dsh.total_records': 'Total Records',
        'dsh.current_cols': 'Current File Columns',
        'dsh.saved_filters_lbl': 'Saved Filters',
        'dsh.my_files': 'My Files',
        'dsh.no_files': 'No files. Upload your first file!',
        'dsh.record': 'record',
        'dsh.analyze_file': 'Analyze File',
        'dsh.delete_file_confirm': 'Delete this file?',
        'dsh.saved_filters_title': 'Saved Filters',
        'dsh.choose_file': 'Select a file to start analysis',
        'dsh.or_upload': 'Or upload a new Excel file',
        'dsh.upload_excel': 'Upload Excel File',
        'dsh.filter_builder': 'Filter Builder',
        'dsh.logic_lbl': 'Logic:',
        'dsh.add_condition': 'Add Condition',
        'dsh.apply_filter': 'Apply Filter',
        'dsh.reset_filters': 'Reset',
        'dsh.save_filter': 'Save Filter',
        'dsh.export_excel': 'Export Excel',
        'dsh.filter_name_ph': 'Filter name...',
        'dsh.save': 'Save',
        'dsh.cancel': 'Cancel',
        'dsh.summary_title': 'Results Summary',
        'dsh.chart_title': 'Chart',
        'dsh.distribution': 'Distribution',
        'dsh.data_title': 'Data',
        'dsh.search_ph': 'Search in table...',
        'dsh.preview_info': 'Showing first 5 records (preview)',
        'dsh.col_placeholder': '-- Select Column --',
        'dsh.no_results': 'No results match the criteria',
        'dsh.total_results': 'Total Results',
        'dsh.sum': 'Sum',
        'dsh.avg': 'Average',
        'dsh.min': 'Min',
        'dsh.max': 'Max',
        'dsh.showing': 'Showing',
        'dsh.of': 'from',
        'dsh.enter_filter_name': 'Please enter a filter name',
        'dsh.filter_saved_ok': 'Filter saved successfully!',
        // User Management
        'um.title': 'User Management',
        'um.subtitle': 'Add, edit and delete users and manage their permissions',
        'um.add_user': 'Add User',
        'um.col_name': 'Name',
        'um.col_email': 'Email',
        'um.col_role': 'Role',
        'um.col_date': 'Created At',
        'um.col_actions': 'Actions',
        'um.you': 'You',
        'um.role_admin': 'Admin',
        'um.role_user': 'User',
        'um.total': 'Total Users',
        'um.no_users': 'No users found',
        'um.create_title': 'Add New User',
        'um.fullname': 'Full Name',
        'um.fullname_ph': 'Enter full name',
        'um.password': 'Password',
        'um.pwd_ph': 'At least 6 characters',
        'um.cancel': 'Cancel',
        'um.create_btn': 'Create',
        'um.edit_title': 'Edit User',
        'um.new_pwd': 'New Password',
        'um.new_pwd_ph': 'Leave empty to keep current',
        'um.optional': 'Optional',
        'um.save': 'Save',
        'um.delete_title': 'Delete User',
        'um.delete_confirm': 'Are you sure you want to delete',
        'um.delete_warning': 'This action cannot be undone.',
        'um.delete_btn': 'Delete',
    }
};

let currentLang = (document.cookie.match(/(?:^|;\s*)lang=([^;]*)/) || [])[1] || 'ar';

function t(key) {
    return TRANSLATIONS[currentLang]?.[key] ?? TRANSLATIONS['ar'][key] ?? key;
}

function applyLanguage(lang) {
    currentLang = lang;
    // Persist in cookie (1 year)
    document.cookie = `lang=${lang};path=/;max-age=31536000;SameSite=Strict;Secure`;

    const html = document.documentElement;
    html.setAttribute('lang', lang === 'ar' ? 'ar' : 'en');
    html.setAttribute('dir', lang === 'ar' ? 'rtl' : 'ltr');

    // Swap Bootstrap CSS
    const bl = document.getElementById('bootstrap-css');
    if (bl) {
        bl.href = lang === 'ar'
            ? 'https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.rtl.min.css'
            : 'https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css';
    }

    // Translate static elements
    document.querySelectorAll('[data-i18n]').forEach(el => {
        const key = el.dataset.i18n;
        const attr = el.dataset.i18nAttr;
        if (attr) el.setAttribute(attr, t(key));
        else el.textContent = t(key);
    });

    // Update toggle button
    const btn = document.getElementById('langToggleBtn');
    if (btn) btn.innerHTML = lang === 'ar'
        ? '<i class="fas fa-globe me-1"></i>English'
        : '<i class="fas fa-globe me-1"></i>عربي';

    // Let pages react to language change
    document.dispatchEvent(new CustomEvent('langChanged', { detail: { lang } }));
}

function toggleLanguage() {
    applyLanguage(currentLang === 'ar' ? 'en' : 'ar');
}

// Apply translations on first load (static elements only, direction already set by server)
document.addEventListener('DOMContentLoaded', () => {
    // Translate static elements
    document.querySelectorAll('[data-i18n]').forEach(el => {
        const key = el.dataset.i18n;
        const attr = el.dataset.i18nAttr;
        if (attr) el.setAttribute(attr, t(key));
        else el.textContent = t(key);
    });
    // Set toggle button label
    const btn = document.getElementById('langToggleBtn');
    if (btn) btn.innerHTML = currentLang === 'ar'
        ? '<i class="fas fa-globe me-1"></i>English'
        : '<i class="fas fa-globe me-1"></i>عربي';
});
