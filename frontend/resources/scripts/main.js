// Global variables
let currentUser = null;
let currentUserRole = null;

// API base URL
const API_BASE = 'http://localhost:5271/api';

// Global error handler
function handleApiError(error, context = '', correlationId = null) {
    console.error('=== API Error ===');
    console.error('Context:', context);
    if (correlationId) {
        console.error('Correlation ID:', correlationId);
    }
    console.error('Error:', error);
    console.error('Error Type:', error.constructor.name);
    if (error.stack) {
        console.error('Stack Trace:', error.stack);
    }
    console.error('=== End API Error ===');
}

// API wrapper with error handling and logging
async function apiCall(url, options = {}) {
    const fullUrl = url.startsWith('http') ? url : `${API_BASE}${url}`;
    const requestId = Math.random().toString(36).substring(7);
    
    console.log(`%c[API Request ${requestId}]`, 'color: #4CAF50; font-weight: bold');
    console.log('URL:', fullUrl);
    console.log('Method:', options.method || 'GET');
    console.log('Headers:', options.headers);
    if (options.body) {
        console.log('Body:', JSON.parse(options.body));
    }
    console.log('Timestamp:', new Date().toISOString());
    
    try {
        const response = await fetch(fullUrl, options);
        
        // Get correlation ID from response headers
        const correlationId = response.headers.get('X-Correlation-ID');
        
        console.log(`%c[API Response ${requestId}]`, 'color: #2196F3; font-weight: bold');
        console.log('Status:', response.status, response.statusText);
        console.log('Correlation ID:', correlationId);
        console.log('Timestamp:', new Date().toISOString());
        
        let data;
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
            data = await response.json();
            console.log('Response Data:', data);
        } else {
            data = await response.text();
            console.log('Response Text:', data);
        }
        
        if (!response.ok) {
            console.error(`%c[API Error ${requestId}]`, 'color: #f44336; font-weight: bold');
            console.error('Error Response:', data);
            console.error('Correlation ID:', correlationId);
            
            // Create detailed error message
            const errorMessage = data.message || data.error || `HTTP ${response.status}: ${response.statusText}`;
            const errorCode = data.errorCode || 'UNKNOWN_ERROR';
            
            throw {
                message: errorMessage,
                errorCode: errorCode,
                correlationId: correlationId,
                statusCode: response.status,
                fullError: data,
                response: response
            };
        }
        
        return data;
    } catch (error) {
        if (error.statusCode) {
            // Already formatted error from API response (including 401, 404, etc.)
            handleApiError(error, `API call to ${fullUrl}`, error.correlationId);
            throw error;
        } else {
            // Network error or other exception
            console.error(`%c[Network Error ${requestId}]`, 'color: #f44336; font-weight: bold');
            console.error('Error:', error.message);
            console.error('Stack:', error.stack);
            
            handleApiError(error, `Network error calling ${fullUrl}`);
            throw {
                message: 'Network error. Please check your connection.',
                errorCode: 'NETWORK_ERROR',
                correlationId: null,
                originalError: error
            };
        }
    }
}

// Display error to user with technical details
function displayError(errorDiv, error) {
    if (!errorDiv) {
        console.error('Error div not found, displaying error in console:', error);
        return;
    }
    
    errorDiv.classList.remove('d-none');
    
    let errorHtml = `<strong>Error:</strong> ${error.message || 'An error occurred'}`;
    
    if (error.errorCode) {
        errorHtml += `<br><small><strong>Error Code:</strong> ${error.errorCode}</small>`;
    }
    
    if (error.correlationId) {
        errorHtml += `<br><small><strong>Correlation ID:</strong> <code>${error.correlationId}</code></small>`;
        errorHtml += `<br><small class="text-muted">Please include this ID when reporting the issue.</small>`;
    }
    
    if (error.statusCode) {
        errorHtml += `<br><small><strong>Status Code:</strong> ${error.statusCode}</small>`;
    }
    
    errorDiv.innerHTML = errorHtml;
}

// Initialize the application
document.addEventListener('DOMContentLoaded', function() {
    initializeApp();
    updateNavigation();
});

// Initialize the application
async function initializeApp() {
    // Check if user is logged in (check session storage)
    const userId = sessionStorage.getItem('userId');
    const userRole = sessionStorage.getItem('userRole');
    const userName = sessionStorage.getItem('userName');

    if (userId && userRole && userName) {
        currentUser = {
            // normalized keys
            id: userId,
            userId,
            role: userRole,
            userRole,
            name: userName,
            userName
        };
        currentUserRole = userRole;
        updateNavigation();
    }
}

// Update navigation based on user state
function updateNavigation() {
    const authNav = document.getElementById('authNav');
    const mainNav = document.querySelector('.navbar-nav.me-auto');
    if (!authNav || !mainNav) return;

    if (currentUser) {
        // User is logged in
        const displayName = currentUser.userName || currentUser.name || sessionStorage.getItem('userName') || 'User';
        const displayRole = currentUser.userRole || currentUser.role || sessionStorage.getItem('userRole') || '';
        authNav.innerHTML = `
            <li class="nav-item">
                <span class="nav-link">Welcome, ${displayName} (${displayRole})</span>
            </li>
            <li class="nav-item">
                <a class="nav-link" href="#" onclick="logout()">
                    <i class="fas fa-sign-out-alt me-1"></i>Logout
                </a>
            </li>
        `;

        // Add role-specific navigation
        let roleNav = '';
        switch (currentUser.userRole) {
            case 'Admin':
                roleNav = `
                    <li class="nav-item">
                        <a class="nav-link" href="#" onclick="showAdminDashboard()">
                            <i class="fas fa-tachometer-alt me-1"></i>Dashboard
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" href="#" onclick="showAdminReports()">
                            <i class="fas fa-chart-bar me-1"></i>Reports
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" href="#" onclick="showAllLessons()">
                            <i class="fas fa-calendar me-1"></i>All Lessons
                        </a>
                    </li>
                `;
                break;
            case 'Teacher':
                roleNav = `
                    <li class="nav-item">
                        <a class="nav-link" href="#" onclick="showTeacherDashboard()">
                            <i class="fas fa-tachometer-alt me-1"></i>Dashboard
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" href="#" onclick="showTeacherAvailability()">
                            <i class="fas fa-calendar-plus me-1"></i>Availability
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" href="#" onclick="showTeacherLessons()">
                            <i class="fas fa-calendar-check me-1"></i>My Lessons
                        </a>
                    </li>
                `;
                break;
            case 'Student':
                roleNav = `
                    <li class="nav-item">
                        <a class="nav-link" href="#" onclick="showStudentDashboard()">
                            <i class="fas fa-tachometer-alt me-1"></i>Dashboard
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" href="#" onclick="showBookLesson()">
                            <i class="fas fa-calendar-plus me-1"></i>Book Lesson
                        </a>
                    </li>
                `;
                break;
        }
        mainNav.innerHTML = roleNav;
    } else {
        // User is not logged in
        authNav.innerHTML = `
            <li class="nav-item">
                <a class="nav-link" href="#" onclick="showLoginModal()">
                    <i class="fas fa-sign-in-alt me-1"></i>Login
                </a>
            </li>
            <li class="nav-item">
                <a class="nav-link" href="#" onclick="showRegisterModal()">
                    <i class="fas fa-user-plus me-1"></i>Register
                </a>
            </li>
        `;
        mainNav.innerHTML = '';
    }
}

// Modal functions
function showLoginModal() {
    document.getElementById('loginModal').querySelector('.modal-title').innerHTML =
        '<i class="fas fa-sign-in-alt text-primary me-2"></i>Login';
    document.getElementById('loginError').classList.add('d-none');
    document.getElementById('loginEmail').value = '';
    document.getElementById('loginPassword').value = '';
    new bootstrap.Modal(document.getElementById('loginModal')).show();
}

function handleRoleChange(role) {
    // Update modal title
    const modalTitle = document.querySelector('#registerModal .modal-title');
    if (modalTitle) {
        modalTitle.innerHTML = `<i class="fas fa-user-plus text-primary me-2"></i>Register as ${role}`;
    }

    // Show/hide role-specific fields
    const teacherBioGroup = document.getElementById('teacherBioGroup');
    const referralSourceGroup = document.getElementById('referralSourceGroup');

    if (teacherBioGroup && referralSourceGroup) {
        if (role === 'Teacher') {
            teacherBioGroup.style.display = 'block';
            referralSourceGroup.style.display = 'none';
        } else {
            teacherBioGroup.style.display = 'none';
            referralSourceGroup.style.display = 'block';
        }
    }
}

function showRegisterModal(role = 'Student') {
    const modalElement = document.getElementById('registerModal');
    if (!modalElement) {
        console.error('Register modal not found in DOM');
        return;
    }
    
    const modal = new bootstrap.Modal(modalElement);
    
    // Get radio button elements
    const roleStudentElement = document.getElementById('roleStudent');
    const roleTeacherElement = document.getElementById('roleTeacher');
    
    // Set the appropriate radio button
    if (roleStudentElement && roleTeacherElement) {
        if (role === 'Teacher') {
            roleTeacherElement.checked = true;
            roleStudentElement.checked = false;
        } else {
            roleStudentElement.checked = true;
            roleTeacherElement.checked = false;
        }
    }

    // Update fields based on role
    handleRoleChange(role);

    // Clear form
    const errorDiv = document.getElementById('registerError');
    if (errorDiv) errorDiv.classList.add('d-none');
    
    const nameInput = document.getElementById('registerName');
    if (nameInput) nameInput.value = '';
    
    const emailInput = document.getElementById('registerEmail');
    if (emailInput) emailInput.value = '';
    
    const passwordInput = document.getElementById('registerPassword');
    if (passwordInput) passwordInput.value = '';
    
    const contactInput = document.getElementById('registerContactInfo');
    if (contactInput) contactInput.value = '';
    
    const instrumentInput = document.getElementById('registerInstrument');
    if (instrumentInput) instrumentInput.value = '';
    
    const bioInput = document.getElementById('registerBio');
    if (bioInput) bioInput.value = '';
    
    const referralInput = document.getElementById('registerReferralSource');
    if (referralInput) referralInput.value = 'Social Media';

    modal.show();
}

// Authentication functions
async function loginUser() {
    const email = document.getElementById('loginEmail').value;
    const password = document.getElementById('loginPassword').value;
    const errorDiv = document.getElementById('loginError');

    console.log('=== Login Attempt ===');
    console.log('Email:', email);
    console.log('Timestamp:', new Date().toISOString());

    if (!email || !password) {
        const error = { message: 'Please enter both email and password.', errorCode: 'VALIDATION_ERROR' };
        displayError(errorDiv, error);
        console.warn('Login validation failed: Missing email or password');
        return;
    }

    try {
        const result = await apiCall('/home/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ email, password })
        });

        if (result.success) {
            console.log('Login successful for:', email);
            console.log('User ID:', result.user.id);
            console.log('Role:', result.user.role);

            // Store user data in session storage
            sessionStorage.setItem('userId', result.user.id);
            sessionStorage.setItem('userRole', result.user.role);
            sessionStorage.setItem('userName', result.user.name);

            // Update current user state
            currentUser = {
                id: result.user.id,
                userId: result.user.id,
                role: result.user.role,
                userRole: result.user.role,
                name: result.user.name,
                userName: result.user.name
            };
            currentUserRole = currentUser.userRole;

            // Close modal and update navigation
            bootstrap.Modal.getInstance(document.getElementById('loginModal')).hide();
            updateNavigation();

            // Redirect to appropriate dashboard
            redirectToDashboard(result.user.role);
        } else {
            displayError(errorDiv, { message: result.message, errorCode: 'LOGIN_FAILED' });
        }
    } catch (error) {
        displayError(errorDiv, error);
        console.error('Login error details:', error);
    }
}

async function registerUser() {
    const name = document.getElementById('registerName')?.value || '';
    const email = document.getElementById('registerEmail')?.value || '';
    const password = document.getElementById('registerPassword')?.value || '';
    
    // Get role from radio buttons with null checks
    const roleStudent = document.getElementById('roleStudent');
    const roleTeacher = document.getElementById('roleTeacher');
    
    let role = 'Student'; // Default to Student
    if (roleTeacher && roleTeacher.checked) {
        role = 'Teacher';
    } else if (roleStudent && roleStudent.checked) {
        role = 'Student';
    } else {
        // Fallback: try to get checked radio by name
        const checkedRole = document.querySelector('input[name="registerRole"]:checked');
        if (checkedRole) {
            role = checkedRole.value;
        }
    }
    
    const contactInfo = document.getElementById('registerContactInfo')?.value || '';
    const instrument = document.getElementById('registerInstrument')?.value || '';
    const bio = document.getElementById('registerBio')?.value || '';
    const referralSource = document.getElementById('registerReferralSource')?.value || '';

    const errorDiv = document.getElementById('registerError');

    console.log('=== Registration Attempt ===');
    console.log('Email:', email);
    console.log('Role:', role);
    console.log('Timestamp:', new Date().toISOString());

    if (!name || !email || !password || !instrument) {
        const error = { message: 'Please fill in all required fields.', errorCode: 'VALIDATION_ERROR' };
        displayError(errorDiv, error);
        console.warn('Registration validation failed: Missing required fields');
        return;
    }

    if (password.length < 6) {
        const error = { message: 'Password must be at least 6 characters long.', errorCode: 'PASSWORD_TOO_SHORT' };
        displayError(errorDiv, error);
        console.warn('Registration validation failed: Password too short');
        return;
    }

    try {
        const requestData = {
            name,
            email,
            password,
            role,
            contactInfo,
            instrument,
            bio: bio || "",
            referralSource: referralSource || "Other"
        };

        const result = await apiCall('/home/register', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(requestData)
        });

        if (result.success) {
            console.log('Registration successful for:', email);
            console.log('Auto-logging in user...');
            
            // Auto-login the user after successful registration
            try {
                const loginResult = await apiCall('/home/login', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({ email, password })
                });

                if (loginResult.success) {
                    console.log('Auto-login successful');
                    console.log('User ID:', loginResult.user.id);
                    console.log('Role:', loginResult.user.role);

                    // Store user data in session storage
                    sessionStorage.setItem('userId', loginResult.user.id);
                    sessionStorage.setItem('userRole', loginResult.user.role);
                    sessionStorage.setItem('userName', loginResult.user.name);

                    // Update current user state
                    currentUser = {
                        id: loginResult.user.id,
                        userId: loginResult.user.id,
                        role: loginResult.user.role,
                        userRole: loginResult.user.role,
                        name: loginResult.user.name,
                        userName: loginResult.user.name
                    };
                    currentUserRole = currentUser.userRole;

                    // Close modal
                    bootstrap.Modal.getInstance(document.getElementById('registerModal')).hide();
                    
                    // Redirect to appropriate dashboard
                    console.log('Redirecting to dashboard for role:', loginResult.user.role);
                    redirectToDashboard(loginResult.user.role);
                } else {
                    // If auto-login fails, show success message and ask user to login
                    bootstrap.Modal.getInstance(document.getElementById('registerModal')).hide();
                    alert('Registration successful! Please log in.');
                }
            } catch (loginError) {
                console.error('Auto-login failed after registration:', loginError);
                // If auto-login fails, still show success and ask user to login
                bootstrap.Modal.getInstance(document.getElementById('registerModal')).hide();
                alert('Registration successful! Please log in.');
            }
        } else {
            displayError(errorDiv, { message: result.message, errorCode: 'REGISTRATION_FAILED' });
        }
    } catch (error) {
        displayError(errorDiv, error);
        console.error('Registration error details:', error);
    }
}

function logout() {
    sessionStorage.removeItem('userId');
    sessionStorage.removeItem('userRole');
    sessionStorage.removeItem('userName');
    currentUser = null;
    currentUserRole = null;
    updateNavigation();
    window.location.href = 'index.html';
}

function redirectToDashboard(role) {
    // For now, we'll redirect to the backend controllers which will handle the views
    // In a real application, you might want to load dashboard content dynamically
    switch (role) {
        case 'Admin':
            window.location.href = '/admin.html';
            break;
        case 'Teacher':
            window.location.href = '/teacher.html';
            break;
        case 'Student':
            window.location.href = '/student.html';
            break;
        default:
            window.location.href = 'index.html';
    }
}

// Dashboard functions
function showAdminDashboard() {
    window.location.href = 'admin.html';
}

function showAdminReports() {
    window.location.href = '/admin.html';
}

function showAllLessons() {
    window.location.href = '/admin.html';
}

function showTeacherDashboard() {
    window.location.href = 'teacher.html';
}

function showTeacherAvailability() {
    window.location.href = '/teacher.html';
}

function showTeacherLessons() {
    window.location.href = '/teacher.html';
}

function showStudentDashboard() {
    window.location.href = 'student.html';
}

function showBookLesson() {
    // This is handled within the student dashboard
    if (window.location.pathname.includes('student.html')) {
        // Show booking section if we're already on student page
        const event = new CustomEvent('showBookLesson');
        document.dispatchEvent(event);
    } else {
        window.location.href = 'student.html';
    }
}
