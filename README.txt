EaxReverbEngine.dll (نسخة معدّلة) - اضافة EQ + Bass + Treble + Crystalliser + Surround + Level

1) ارفع كل محتويات هذا المجلد على GitHub (مع مجلد .github و src) - مستودع جديد او استبدل القديم
2) تبويب Actions -> build-engine -> بعد دقيقة نزّل Artifact باسم EaxReverbEngine (فيه EaxReverbEngine.dll)
3) سكّر برنامج EAX Settings، خذ نسخة احتياطية من EaxReverbEngine.dll القديمة، وبدّلها بالجديدة (نفس مجلد الـ exe)
4) شغّل الـ exe: تفتح نافذة "Graphic Equalizer" جنب البرنامج. الاعدادات تنحفظ تلقائيا في
   %APPDATA%\CreativeEAXSettings\fx.ini
الصوت: التأثيرات تنطبق على خرج المحرك (بعد الـ Reverb). الـ log في engine.log لو صار شي.
