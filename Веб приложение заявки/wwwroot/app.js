/**
 * QuestPortal — Клиентская логика веб-приложения (ASP.NET Core + AJAX)
 */

document.addEventListener('DOMContentLoaded', () => {
  // Инициализация иконок Lucide
  if (window.lucide) {
    lucide.createIcons();
  }

  // Состояние приложения
  let state = {
    currentApplication: null,
    skills: [],
    avatarDataUrl: ''
  };

  // Элементы навигации и экранов
  const screenForm = document.getElementById('screen-form');
  const screenSuccess = document.getElementById('screen-success');
  const screenProfile = document.getElementById('screen-profile');
  const navCreateBtn = document.getElementById('nav-create-btn');
  const navHistoryBtn = document.getElementById('nav-history-btn');
  const navHome = document.getElementById('nav-home');
  const historyCounter = document.getElementById('history-counter');

  // Элементы формы
  const appForm = document.getElementById('application-form');
  const submitBtn = document.getElementById('submit-btn');
  const submitSpinner = document.getElementById('submit-spinner');
  const submitText = document.getElementById('submit-text');
  const submitIcon = document.getElementById('submit-icon');
  const serverErrorBanner = document.getElementById('server-error-banner');
  const serverErrorMessage = document.getElementById('server-error-message');
  const fillSampleBtn = document.getElementById('fill-sample-btn');

  // Аватар
  const avatarInput = document.getElementById('avatar-input');
  const avatarPreview = document.getElementById('avatar-preview');
  const removeAvatarBtn = document.getElementById('remove-avatar-btn');
  const defaultAvatarSvg = `data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='120' height='120' viewBox='0 0 24 24' fill='none' stroke='%2394a3b8' stroke-width='1.5' stroke-linecap='round' stroke-linejoin='round'><path d='M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2'/><circle cx='12' cy='7' r='4'/></svg>`;

  // Теги навыков
  const skillInputField = document.getElementById('skill-input-field');
  const skillsList = document.getElementById('skills-list');

  // Модальное окно истории
  const historyModal = document.getElementById('history-modal');
  const closeHistoryBtn = document.getElementById('close-history-btn');
  const closeHistoryFooterBtn = document.getElementById('close-history-footer-btn');
  const historyList = document.getElementById('history-list');
  const historyEmpty = document.getElementById('history-empty');
  const clearAllHistoryBtn = document.getElementById('clear-all-history-btn');

  // Кнопки действий
  const viewProfileBtn = document.getElementById('view-profile-btn');
  const successNewAppBtn = document.getElementById('success-new-app-btn');
  const backToFormBtn = document.getElementById('back-to-form-btn');
  const editProfileBtn = document.getElementById('edit-profile-btn');
  const printProfileBtn = document.getElementById('print-profile-btn');
  const copySummaryBtn = document.getElementById('copy-summary-btn');
  const toast = document.getElementById('toast');

  // =========================================================================
  // 1. ПЕРЕКЛЮЧЕНИЕ ЭКРАНОВ
  // =========================================================================
  function showScreen(screenName) {
    screenForm.classList.remove('active');
    screenSuccess.classList.remove('active');
    screenProfile.classList.remove('active');

    navCreateBtn.classList.remove('active');

    if (screenName === 'form') {
      screenForm.classList.add('active');
      navCreateBtn.classList.add('active');
    } else if (screenName === 'success') {
      screenSuccess.classList.add('active');
    } else if (screenName === 'profile') {
      screenProfile.classList.add('active');
    }

    window.scrollTo({ top: 0, behavior: 'smooth' });

    if (window.lucide) {
      lucide.createIcons();
    }
  }

  // =========================================================================
  // 2. УПРАВЛЕНИЕ АВАТАРОМ
  // =========================================================================
  avatarInput.addEventListener('change', (e) => {
    const file = e.target.files[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      showToast('Пожалуйста, выберите файл изображения');
      return;
    }

    const reader = new FileReader();
    reader.onload = (event) => {
      state.avatarDataUrl = event.target.result;
      avatarPreview.src = state.avatarDataUrl;
      removeAvatarBtn.style.display = 'inline-flex';
    };
    reader.readAsDataURL(file);
  });

  removeAvatarBtn.addEventListener('click', () => {
    state.avatarDataUrl = '';
    avatarPreview.src = defaultAvatarSvg;
    avatarInput.value = '';
    removeAvatarBtn.style.display = 'none';
  });

  // =========================================================================
  // 3. УПРАВЛЕНИЕ НАВЫКАМИ (ТЕГАМИ)
  // =========================================================================
  function renderSkills() {
    skillsList.innerHTML = '';
    state.skills.forEach((skill, index) => {
      const chip = document.createElement('span');
      chip.className = 'skill-chip';
      chip.innerHTML = `
        ${escapeHtml(skill)}
        <button type="button" class="skill-chip-remove" data-index="${index}">
          <i data-lucide="x" style="width: 14px; height: 14px;"></i>
        </button>
      `;
      skillsList.appendChild(chip);
    });

    if (window.lucide) {
      lucide.createIcons();
    }

    // Обработчик удаления
    skillsList.querySelectorAll('.skill-chip-remove').forEach(btn => {
      btn.addEventListener('click', (e) => {
        const idx = parseInt(e.currentTarget.getAttribute('data-index'), 10);
        state.skills.splice(idx, 1);
        renderSkills();
      });
    });
  }

  function addSkill(value) {
    const trimmed = value.trim();
    if (trimmed && !state.skills.includes(trimmed)) {
      state.skills.push(trimmed);
      renderSkills();
    }
  }

  skillInputField.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' || e.key === ',') {
      e.preventDefault();
      addSkill(skillInputField.value);
      skillInputField.value = '';
    }
  });

  skillInputField.addEventListener('blur', () => {
    if (skillInputField.value.trim()) {
      addSkill(skillInputField.value);
      skillInputField.value = '';
    }
  });

  // =========================================================================
  // 4. МАСКА НОМЕРА ТЕЛЕФОНА
  // =========================================================================
  const phoneInput = document.getElementById('phone');
  phoneInput.addEventListener('input', (e) => {
    let input = e.target.value.replace(/\D/g, '');
    if (!input) {
      e.target.value = '';
      return;
    }

    if (['7', '8', '9'].indexOf(input[0]) > -1) {
      if (input[0] === '9') input = '7' + input;
      const firstSymbols = (input[0] === '8') ? '8' : '+7';
      let formatted = firstSymbols + ' ';

      if (input.length > 1) {
        formatted += '(' + input.substring(1, 4);
      }
      if (input.length >= 5) {
        formatted += ') ' + input.substring(4, 7);
      }
      if (input.length >= 8) {
        formatted += '-' + input.substring(7, 9);
      }
      if (input.length >= 10) {
        formatted += '-' + input.substring(9, 11);
      }
      e.target.value = formatted;
    } else {
      e.target.value = '+' + input.substring(0, 16);
    }
  });

  // =========================================================================
  // 5. ДЕМО ДАННЫЕ
  // =========================================================================
  fillSampleBtn.addEventListener('click', () => {
    document.getElementById('fullName').value = 'Смирнов Алексей Владимирович';
    document.getElementById('birthDate').value = '1995-04-12';
    document.getElementById('gender').value = 'Мужской';
    document.getElementById('city').value = 'Москва';
    document.getElementById('phone').value = '+7 (926) 543-21-98';
    document.getElementById('email').value = 'alex.smirnov.dev@mail.ru';
    document.getElementById('telegram').value = '@alex_dotnet';
    document.getElementById('category').value = 'IT & Программирование';
    document.getElementById('desiredRole').value = 'Senior C# / ASP.NET Core Разработчик';
    document.getElementById('experience').value = 'От 3 до 5 лет';
    document.getElementById('workFormat').value = 'Удалённая работа';
    document.getElementById('salaryExpectation').value = '230 000 ₽';
    document.getElementById('aboutMe').value = 'Более 5 лет проектирую и разрабатываю надёжные веб-сервисы на ASP.NET Core, C# и современных фронтенд-технологиях (AJAX, WebSockets, REST). Имею опыт создания микросервисов, оптимизации запросов Entity Framework / PostgreSQL и настройки CI/CD пайплайнов. Открыт к сложным задачам и командной работе.';

    state.skills = ['C#', 'ASP.NET Core', 'Web API', 'AJAX', 'PostgreSQL', 'Docker', 'Git', 'Redis'];
    renderSkills();

    clearValidationErrors();
    showToast('Демо-данные успешно подставлены в форму');
  });

  // =========================================================================
  // 6. ВАЛИДАЦИЯ И AJAX ОТПРАВКА В ASP.NET
  // =========================================================================
  function clearValidationErrors() {
    document.querySelectorAll('.form-group, .checkbox-group').forEach(el => {
      el.classList.remove('has-error');
    });
    document.querySelectorAll('.field-error').forEach(el => {
      el.textContent = '';
    });
    serverErrorBanner.style.display = 'none';
  }

  function setFieldError(fieldName, message) {
    const inputEl = document.getElementById(fieldName);
    const errorEl = document.getElementById('error-' + fieldName);
    if (inputEl) {
      const parentGroup = inputEl.closest('.form-group') || inputEl.closest('.checkbox-group');
      if (parentGroup) parentGroup.classList.add('has-error');
    }
    if (errorEl) {
      errorEl.textContent = message;
    }
  }

  function validateForm() {
    clearValidationErrors();
    let isValid = true;

    const fullName = document.getElementById('fullName').value.trim();
    if (!fullName) {
      setFieldError('fullName', 'Пожалуйста, введите фамилию, имя и отчество');
      isValid = false;
    } else if (fullName.length < 3) {
      setFieldError('fullName', 'ФИО должно содержать минимум 3 символа');
      isValid = false;
    }

    const birthDate = document.getElementById('birthDate').value;
    if (!birthDate) {
      setFieldError('birthDate', 'Укажите дату рождения');
      isValid = false;
    }

    const gender = document.getElementById('gender').value;
    if (!gender) {
      setFieldError('gender', 'Выберите пол');
      isValid = false;
    }

    const city = document.getElementById('city').value.trim();
    if (!city) {
      setFieldError('city', 'Укажите город проживания');
      isValid = false;
    }

    const phone = document.getElementById('phone').value.trim();
    if (!phone || phone.length < 10) {
      setFieldError('phone', 'Введите контактный номер телефона');
      isValid = false;
    }

    const email = document.getElementById('email').value.trim();
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!email || !emailRegex.test(email)) {
      setFieldError('email', 'Введите корректный email (например, user@domain.ru)');
      isValid = false;
    }

    const category = document.getElementById('category').value;
    if (!category) {
      setFieldError('category', 'Выберите сферу деятельности');
      isValid = false;
    }

    const desiredRole = document.getElementById('desiredRole').value.trim();
    if (!desiredRole) {
      setFieldError('desiredRole', 'Укажите желаемую должность');
      isValid = false;
    }

    const experience = document.getElementById('experience').value;
    if (!experience) {
      setFieldError('experience', 'Укажите ваш опыт работы');
      isValid = false;
    }

    const consentCheck = document.getElementById('consentCheck').checked;
    if (!consentCheck) {
      setFieldError('consentCheck', 'Необходимо дать согласие на обработку данных');
      isValid = false;
    }

    return isValid;
  }

  appForm.addEventListener('submit', async (e) => {
    e.preventDefault();

    if (!validateForm()) {
      const firstError = document.querySelector('.has-error');
      if (firstError) {
        firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }
      return;
    }

    // Сборка объекта заявки для отправки через AJAX в ASP.NET
    const payload = {
      id: document.getElementById('appId').value || '',
      fullName: document.getElementById('fullName').value.trim(),
      birthDate: document.getElementById('birthDate').value,
      gender: document.getElementById('gender').value,
      city: document.getElementById('city').value.trim(),
      phone: document.getElementById('phone').value.trim(),
      email: document.getElementById('email').value.trim(),
      telegram: document.getElementById('telegram').value.trim(),
      category: document.getElementById('category').value,
      desiredRole: document.getElementById('desiredRole').value.trim(),
      experience: document.getElementById('experience').value,
      workFormat: document.getElementById('workFormat').value,
      salaryExpectation: document.getElementById('salaryExpectation').value.trim(),
      skills: state.skills,
      aboutMe: document.getElementById('aboutMe').value.trim(),
      photoDataUrl: state.avatarDataUrl
    };

    // Блокировка кнопки и отображение спиннера AJAX-запроса
    setSubmitting(true);

    try {
      // === AJAX ЗАПРОС К ASP.NET CORE КОНТРОЛЛЕРУ ===
      const response = await fetch('/api/applications', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        },
        body: JSON.stringify(payload)
      });

      const result = await response.json();

      if (!response.ok) {
        // Ошибка валидации со стороны сервера ASP.NET
        serverErrorBanner.style.display = 'flex';
        serverErrorMessage.textContent = result.message || 'Ошибка обработки формы на сервере.';

        if (result.errors) {
          for (const key of Object.keys(result.errors)) {
            const fieldKey = key.charAt(0).toLowerCase() + key.slice(1);
            setFieldError(fieldKey, result.errors[key][0]);
          }
        }
        serverErrorBanner.scrollIntoView({ behavior: 'smooth', block: 'center' });
        return;
      }

      // Заявка успешно сохранена на сервере!
      state.currentApplication = result.data;

      // Обновляем экран успешной отправки
      populateSuccessScreen(result.data);

      // Заполняем анкету
      populateProfileScreen(result.data);

      // Переключаемся на экран успеха
      showScreen('success');

      // Праздничный салют конфетти!
      launchConfetti();

      // Обновляем счётчик истории
      loadApplicationsCount();

    } catch (err) {
      console.error('AJAX Error:', err);
      serverErrorBanner.style.display = 'flex';
      serverErrorMessage.textContent = 'Ошибка сетевого соединения с ASP.NET сервером. Убедитесь, что сервер запущен.';
      serverErrorBanner.scrollIntoView({ behavior: 'smooth', block: 'center' });
    } finally {
      setSubmitting(false);
    }
  });

  function setSubmitting(isSubmitting) {
    if (isSubmitting) {
      submitBtn.disabled = true;
      submitSpinner.style.display = 'inline-block';
      submitIcon.style.display = 'none';
      submitText.textContent = 'Отправка через AJAX...';
    } else {
      submitBtn.disabled = false;
      submitSpinner.style.display = 'none';
      submitIcon.style.display = 'inline-block';
      submitText.textContent = 'Отправить через AJAX';
    }
  }

  // =========================================================================
  // 7. ЗАПОЛНЕНИЕ ЭКРАНА УСПЕХА
  // =========================================================================
  function populateSuccessScreen(app) {
    document.getElementById('success-app-id').textContent = app.id || '#APP-2026-0000';
    
    const dateObj = app.createdAt ? new Date(app.createdAt) : new Date();
    document.getElementById('success-timestamp').textContent = formatDateTime(dateObj);
  }

  // =========================================================================
  // 8. ЗАПОЛНЕНИЕ АНКЕТЫ СОИСКАТЕЛЯ
  // =========================================================================
  function populateProfileScreen(app) {
    // Верхняя плашка
    document.getElementById('profile-id-display').textContent = `Заявка № ${app.id}`;
    const dateObj = app.createdAt ? new Date(app.createdAt) : new Date();
    document.getElementById('profile-date-display').textContent = formatDate(dateObj);

    // Фото
    const avatarImg = document.getElementById('profile-avatar-img');
    if (app.photoDataUrl) {
      avatarImg.src = app.photoDataUrl;
    } else {
      avatarImg.src = defaultAvatarSvg;
    }

    // Имя и роль
    document.getElementById('profile-full-name').textContent = app.fullName;
    document.getElementById('profile-target-role').textContent = app.desiredRole;
    document.getElementById('profile-format-chip').innerHTML = `<i data-lucide="laptop"></i> ${escapeHtml(app.workFormat)}`;

    // Быстрые данные
    document.getElementById('profile-city').textContent = app.city;
    
    let ageStr = '';
    if (app.age !== null && app.age !== undefined) {
      ageStr = `${app.age} ${getAgeWord(app.age)} (${app.gender})`;
    } else {
      ageStr = `${app.gender}`;
    }
    document.getElementById('profile-age').textContent = ageStr;
    document.getElementById('profile-experience').textContent = `Опыт: ${app.experience}`;
    document.getElementById('profile-salary').textContent = app.salaryExpectation || 'По договорённости';

    // Контакты
    const phoneLink = document.getElementById('profile-phone-link');
    phoneLink.textContent = app.phone;
    phoneLink.href = `tel:${app.phone.replace(/[^\d+]/g, '')}`;

    const emailLink = document.getElementById('profile-email-link');
    emailLink.textContent = app.email;
    emailLink.href = `mailto:${app.email}`;

    const tgItem = document.getElementById('contact-telegram-item');
    const tgLink = document.getElementById('profile-telegram-link');
    if (app.telegram) {
      tgItem.style.display = 'flex';
      tgLink.textContent = app.telegram;
      const cleanTg = app.telegram.replace('@', '');
      tgLink.href = `https://t.me/${cleanTg}`;
    } else {
      tgItem.style.display = 'none';
    }

    // Навыки
    const skillsCloud = document.getElementById('profile-skills-cloud');
    skillsCloud.innerHTML = '';
    if (app.skills && app.skills.length > 0) {
      app.skills.forEach(skill => {
        const badge = document.createElement('span');
        badge.className = 'profile-skill-badge';
        badge.textContent = skill;
        skillsCloud.appendChild(badge);
      });
    } else {
      skillsCloud.innerHTML = '<span style="color: var(--text-muted); font-size: 0.85rem;">Навыки не указаны</span>';
    }

    // Детали
    document.getElementById('profile-field-category').textContent = app.category;
    document.getElementById('profile-field-format').textContent = app.workFormat;
    document.getElementById('profile-field-exp').textContent = app.experience;
    document.getElementById('profile-field-salary').textContent = app.salaryExpectation || 'По договорённости';

    // О себе
    const bioEl = document.getElementById('profile-bio');
    if (app.aboutMe && app.aboutMe.trim()) {
      bioEl.textContent = app.aboutMe;
    } else {
      bioEl.textContent = 'Сопроводительный текст не заполнен.';
    }

    // Подвал
    document.getElementById('profile-footer-stamp').textContent = `Проверено и сохранено: ${formatDateTime(dateObj)}`;
    document.getElementById('profile-footer-id').textContent = app.id;

    if (window.lucide) {
      lucide.createIcons();
    }
  }

  // =========================================================================
  // 9. ДЕЙСТВИЯ С АНКЕТОЙ (ПЕЧАТЬ, РЕДАКТИРОВАНИЕ, КОПИРОВАНИЕ)
  // =========================================================================
  viewProfileBtn.addEventListener('click', () => {
    showScreen('profile');
  });

  successNewAppBtn.addEventListener('click', () => {
    resetForm();
    showScreen('form');
  });

  backToFormBtn.addEventListener('click', () => {
    showScreen('form');
  });

  printProfileBtn.addEventListener('click', () => {
    window.print();
  });

  editProfileBtn.addEventListener('click', () => {
    if (!state.currentApplication) return;
    loadApplicationIntoForm(state.currentApplication);
    showScreen('form');
  });

  copySummaryBtn.addEventListener('click', () => {
    if (!state.currentApplication) return;
    const app = state.currentApplication;
    const text = `
=== АНКЕТА СОИСКАТЕЛЯ [${app.id}] ===
ФИО: ${app.fullName}
Должность: ${app.desiredRole}
Сфера: ${app.category}
Опыт: ${app.experience}
Формат работы: ${app.workFormat}
Ожидания по зарплате: ${app.salaryExpectation || 'Не указано'}
Город: ${app.city}
Контакты:
- Телефон: ${app.phone}
- Email: ${app.email}
- Telegram: ${app.telegram || 'Не указан'}
Ключевые навыки: ${(app.skills || []).join(', ')}

О себе:
${app.aboutMe || 'Нет описания'}
--------------------------------------
Сформировано сервисом QuestPortal (ASP.NET AJAX)
    `.trim();

    navigator.clipboard.writeText(text).then(() => {
      showToast('Анкета скопирована в буфер обмена!');
    }).catch(() => {
      showToast('Не удалось скопировать текст');
    });
  });

  function resetForm() {
    appForm.reset();
    document.getElementById('appId').value = '';
    state.skills = [];
    state.avatarDataUrl = '';
    avatarPreview.src = defaultAvatarSvg;
    removeAvatarBtn.style.display = 'none';
    renderSkills();
    clearValidationErrors();
  }

  function loadApplicationIntoForm(app) {
    document.getElementById('appId').value = app.id || '';
    document.getElementById('fullName').value = app.fullName || '';
    document.getElementById('birthDate').value = app.birthDate || '';
    document.getElementById('gender').value = app.gender || '';
    document.getElementById('city').value = app.city || '';
    document.getElementById('phone').value = app.phone || '';
    document.getElementById('email').value = app.email || '';
    document.getElementById('telegram').value = app.telegram || '';
    document.getElementById('category').value = app.category || '';
    document.getElementById('desiredRole').value = app.desiredRole || '';
    document.getElementById('experience').value = app.experience || '';
    document.getElementById('workFormat').value = app.workFormat || 'Удалённая работа';
    document.getElementById('salaryExpectation').value = app.salaryExpectation || '';
    document.getElementById('aboutMe').value = app.aboutMe || '';

    state.skills = [...(app.skills || [])];
    renderSkills();

    if (app.photoDataUrl) {
      state.avatarDataUrl = app.photoDataUrl;
      avatarPreview.src = app.photoDataUrl;
      removeAvatarBtn.style.display = 'inline-flex';
    } else {
      state.avatarDataUrl = '';
      avatarPreview.src = defaultAvatarSvg;
      removeAvatarBtn.style.display = 'none';
    }

    clearValidationErrors();
    showToast(`Загружены данные заявки ${app.id} для редактирования`);
  }

  // =========================================================================
  // 10. МОДАЛЬНОЕ ОКНО ИСТОРИИ (AJAX GET И DELETE)
  // =========================================================================
  navHistoryBtn.addEventListener('click', openHistoryModal);
  closeHistoryBtn.addEventListener('click', closeHistoryModal);
  closeHistoryFooterBtn.addEventListener('click', closeHistoryModal);
  historyModal.addEventListener('click', (e) => {
    if (e.target === historyModal) closeHistoryModal();
  });

  function closeHistoryModal() {
    historyModal.style.display = 'none';
  }

  async function openHistoryModal() {
    historyModal.style.display = 'flex';
    await fetchHistoryApplications();
  }

  async function loadApplicationsCount() {
    try {
      const res = await fetch('/api/applications');
      if (res.ok) {
        const json = await res.json();
        const count = json.count || (json.data ? json.data.length : 0);
        historyCounter.textContent = count;
      }
    } catch (e) {
      console.warn('Could not fetch applications count:', e);
    }
  }

  async function fetchHistoryApplications() {
    try {
      historyList.innerHTML = '<div style="text-align: center; padding: 2rem; color: var(--text-muted);">Загрузка заявок с сервера ASP.NET...</div>';
      
      const res = await fetch('/api/applications');
      if (!res.ok) throw new Error('Ошибка сервера');
      
      const json = await res.json();
      const list = json.data || [];

      historyCounter.textContent = list.length;

      if (list.length === 0) {
        historyEmpty.style.display = 'block';
        historyList.innerHTML = '';
        return;
      }

      historyEmpty.style.display = 'none';
      historyList.innerHTML = '';

      list.forEach(app => {
        const card = document.createElement('div');
        card.className = 'history-item-card';
        card.innerHTML = `
          <div class="history-item-info">
            <span class="history-item-name">${escapeHtml(app.fullName)}</span>
            <span class="history-item-meta">
              <strong>${escapeHtml(app.id)}</strong> &bull; ${escapeHtml(app.desiredRole)} &bull; ${formatDate(new Date(app.createdAt))}
            </span>
          </div>
          <div class="history-item-actions">
            <button type="button" class="btn btn-secondary btn-view" data-id="${app.id}">
              <i data-lucide="eye" style="width: 16px; height: 16px;"></i> Анкета
            </button>
            <button type="button" class="btn-icon btn-delete" data-id="${app.id}" title="Удалить заявку">
              <i data-lucide="trash-2" style="width: 16px; height: 16px; color: var(--danger);"></i>
            </button>
          </div>
        `;
        historyList.appendChild(card);
      });

      if (window.lucide) {
        lucide.createIcons();
      }

      // Кнопки просмотра анкеты
      historyList.querySelectorAll('.btn-view').forEach(btn => {
        btn.addEventListener('click', async (e) => {
          const id = e.currentTarget.getAttribute('data-id');
          await loadAndShowApplication(id);
          closeHistoryModal();
        });
      });

      // Кнопки удаления анкеты
      historyList.querySelectorAll('.btn-delete').forEach(btn => {
        btn.addEventListener('click', async (e) => {
          const id = e.currentTarget.getAttribute('data-id');
          if (confirm(`Удалить заявку ${id}?`)) {
            await deleteApplication(id);
          }
        });
      });

    } catch (e) {
      console.error(e);
      historyList.innerHTML = '<div style="color: var(--danger); text-align: center; padding: 2rem;">Не удалось загрузить заявки с сервера.</div>';
    }
  }

  async function loadAndShowApplication(id) {
    try {
      const res = await fetch(`/api/applications/${id}`);
      if (!res.ok) throw new Error('Заявка не найдена');
      const json = await res.json();
      state.currentApplication = json.data;
      populateProfileScreen(json.data);
      showScreen('profile');
    } catch (e) {
      showToast('Не удалось загрузить данные анкеты');
    }
  }

  async function deleteApplication(id) {
    try {
      const res = await fetch(`/api/applications/${id}`, { method: 'DELETE' });
      if (res.ok) {
        showToast(`Заявка ${id} удалена`);
        await fetchHistoryApplications();
      }
    } catch (e) {
      showToast('Ошибка при удалении заявки');
    }
  }

  clearAllHistoryBtn.addEventListener('click', async () => {
    if (confirm('Вы действительно хотите удалить все заявки из базы данных?')) {
      try {
        const res = await fetch('/api/applications', { method: 'DELETE' });
        if (res.ok) {
          showToast('Все заявки успешно очищены');
          await fetchHistoryApplications();
        }
      } catch (e) {
        showToast('Ошибка при очистке');
      }
    }
  });

  navCreateBtn.addEventListener('click', () => {
    showScreen('form');
  });

  navHome.addEventListener('click', () => {
    showScreen('form');
  });

  // =========================================================================
  // 11. ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ
  // =========================================================================
  function launchConfetti() {
    if (typeof confetti === 'function') {
      confetti({
        particleCount: 80,
        spread: 70,
        origin: { y: 0.6 }
      });
      setTimeout(() => {
        confetti({
          particleCount: 50,
          angle: 60,
          spread: 55,
          origin: { x: 0 }
        });
        confetti({
          particleCount: 50,
          angle: 120,
          spread: 55,
          origin: { x: 1 }
        });
      }, 250);
    }
  }

  function showToast(text) {
    toast.textContent = text;
    toast.classList.add('show');
    setTimeout(() => {
      toast.classList.remove('show');
    }, 3200);
  }

  function formatDate(date) {
    return date.toLocaleDateString('ru-RU', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  }

  function formatDateTime(date) {
    return date.toLocaleDateString('ru-RU', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  function getAgeWord(age) {
    const lastDigit = age % 10;
    const lastTwo = age % 100;
    if (lastTwo >= 11 && lastTwo <= 19) return 'лет';
    if (lastDigit === 1) return 'год';
    if (lastDigit >= 2 && lastDigit <= 4) return 'года';
    return 'лет';
  }

  function escapeHtml(str) {
    if (!str) return '';
    return str
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;');
  }

  // Автоматический показ для скриншотов / демонстрации по URL (?demo=form / success / profile)
  const urlParams = new URLSearchParams(window.location.search);
  const demoMode = urlParams.get('demo');
  if (demoMode === 'form') {
    fillSampleBtn.click();
  } else if (demoMode === 'success' || demoMode === 'profile') {
    fillSampleBtn.click();
    const demoApp = {
      id: 'APP-2026-8491',
      createdAt: new Date().toISOString(),
      status: 'На рассмотрении',
      fullName: 'Смирнов Алексей Владимирович',
      birthDate: '1995-04-12',
      age: 31,
      gender: 'Мужской',
      city: 'Москва',
      phone: '+7 (926) 543-21-98',
      email: 'alex.smirnov.dev@mail.ru',
      telegram: '@alex_dotnet',
      category: 'IT & Программирование',
      desiredRole: 'Senior C# / ASP.NET Core Разработчик',
      experience: 'От 3 до 5 лет',
      workFormat: 'Удалённая работа',
      salaryExpectation: '230 000 ₽',
      skills: ['C#', 'ASP.NET Core', 'Web API', 'AJAX', 'PostgreSQL', 'Docker', 'Git', 'Redis'],
      aboutMe: 'Более 5 лет проектирую и разрабатываю надёжные веб-сервисы на ASP.NET Core, C# и современных фронтенд-технологиях (AJAX, WebSockets, REST). Имею опыт создания микросервисов, оптимизации запросов Entity Framework / PostgreSQL и настройки CI/CD пайплайнов.',
      photoDataUrl: ''
    };
    state.currentApplication = demoApp;
    populateSuccessScreen(demoApp);
    populateProfileScreen(demoApp);
    showScreen(demoMode);
  }

  // Загружаем счётчик заявок при старте
  loadApplicationsCount();
});
