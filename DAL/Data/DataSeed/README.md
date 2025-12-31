# Seed Data Files

هذا المجلد يحتوي على ملفات JSON للبيانات الأولية (Seed Data) المستخدمة في اختبار الـ APIs.

## الملفات المتاحة:

### 1. SubscriptionPlans.json
يحتوي على خطط الاشتراك المختلفة (مطابقة لموقع [رافد](https://rafeed.vercel.app/)):
- **المبتدأ**: $50/شهر - حتى 50 موظف - مثالي للشركات الصغيرة
  - تقارير يومية غير محدودة - تخطيط أسبوعي - لوحة تحكم أساسية - دعم عبر البريد الإلكتروني - تخزين 5GB
- **المحترف**: $100/شهر - حتى 100 موظف - للشركات المتوسطة
  - جميع مميزات المبتدأ - تقارير متقدمة ورسوم بيانية - تصدير PDF و Excel - دعم فني أولوية - تخزين 50GB
- **المؤسسات**: حسب الطلب - موظفين غير محدود - للشركات الكبيرة
  - جميع مميزات المحترف - API مخصص ودمج - دعم فني 24/7 - تخزين غير محدود - SSO وأمان متقدم

### 2. AdminUsers.json
يحتوي على بيانات مسؤولي النظام للاختبار:
- admin@rafedd.com / Admin123!@#
- ahmed.admin@rafedd.com / Admin123!@#

### 3. ManagerUsers.json
يحتوي على بيانات المديرين للاختبار مع الاشتراكات:
- manager1@test.com / Manager123!@# - شركة التقنية المتقدمة
- manager2@test.com / Manager123!@# - مؤسسة التطوير الإبداعي
- manager3@test.com / Manager123!@# - مجموعة الخدمات الاستشارية

### 4. EmployeeUsers.json
يحتوي على بيانات الموظفين للاختبار:
- employee1@test.com / Employee123!@# - موظف لدى manager1@test.com
- employee2@test.com / Employee123!@# - موظف لدى manager1@test.com
- employee3@test.com / Employee123!@# - موظف لدى manager2@test.com
- employee4@test.com / Employee123!@# - موظف لدى manager2@test.com
- employee5@test.com / Employee123!@# - موظف لدى manager3@test.com

## كيفية الاستخدام:

### عبر Admin API:
1. **Seed All Data** (جميع البيانات):
   ```http
   POST /api/admin/seed/all
   Authorization: Bearer {AdminToken}
   ```

2. **Seed Subscription Plans فقط**:
   ```http
   POST /api/admin/seed/subscription-plans
   Authorization: Bearer {AdminToken}
   ```

3. **Seed Admin Users فقط**:
   ```http
   POST /api/admin/seed/admin-users
   Authorization: Bearer {AdminToken}
   ```

4. **Seed Manager Users فقط** (مع الاشتراكات):
   ```http
   POST /api/admin/seed/manager-users
   Authorization: Bearer {AdminToken}
   ```

5. **Seed Employee Users فقط**:
   ```http
   POST /api/admin/seed/employee-users
   Authorization: Bearer {AdminToken}
   ```

## ملاحظات مهمة:

- ✅ إذا كان المستخدم موجود بالفعل (نفس البريد الإلكتروني)، لن يتم إنشاؤه مرة أخرى
- ✅ Subscription Plans يتم تحديثها إذا كانت موجودة بالفعل
- ✅ Manager Users يتم إنشاء اشتراكات لهم تلقائياً
- ✅ Employee Users يتم ربطهم بالمدير المحدد في ManagerEmail
- ✅ جميع كلمات المرور هي: `Admin123!@#` أو `Manager123!@#` أو `Employee123!@#`

## تسلسل التنفيذ الموصى به:

1. أولاً: `POST /api/admin/seed/subscription-plans`
2. ثانياً: `POST /api/admin/seed/admin-users`
3. ثالثاً: `POST /api/admin/seed/manager-users`
4. رابعاً: `POST /api/admin/seed/employee-users`

أو استخدم `POST /api/admin/seed/all` لتشغيل كل شيء دفعة واحدة.

