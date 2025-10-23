// Teacher Dashboard JavaScript
let currentTeacherId = 1; // For demo purposes

document.addEventListener('DOMContentLoaded', function() {
    if (window.location.pathname.includes('teacher.html')) {
        loadTeacherDashboard();
        loadTeacherProfile();
        populateTimeDropdown();
        setMinimumDate();
    }
});

// Populate time dropdown with 15-minute increments
function populateTimeDropdown() {
    const timeSelect = document.getElementById('availabilityTime');
    if (!timeSelect) return;
    
    timeSelect.innerHTML = '';
    
    for (let hour = 0; hour < 24; hour++) {
        for (let minute = 0; minute < 60; minute += 15) {
            const hourStr = hour.toString().padStart(2, '0');
            const minuteStr = minute.toString().padStart(2, '0');
            const timeValue = `${hourStr}:${minuteStr}`;
            const option = document.createElement('option');
            option.value = timeValue;
            option.textContent = timeValue;
            timeSelect.appendChild(option);
        }
    }
}

// Set minimum date to today
function setMinimumDate() {
    const dateInput = document.getElementById('availabilityDate');
    if (!dateInput) return;
    
    const today = new Date().toISOString().split('T')[0];
    dateInput.min = today;
    dateInput.value = today;
}

function loadTeacherDashboard() {
    loadUpcomingLessons();
}

async function loadUpcomingLessons() {
    try {
        const response = await fetch(`${API_BASE}/teacher/lessons/${currentTeacherId}`);
        const data = await response.json();

        if (data.success && data.lessons) {
            if (data.lessons.length > 0) {
                let tableHtml = `
                    <table class="table table-hover">
                        <thead>
                            <tr>
                                <th>Date & Time</th>
                                <th>Student</th>
                                <th>Instrument</th>
                                <th>Mode</th>
                                <th>Duration</th>
                                <th>Price</th>
                            </tr>
                        </thead>
                        <tbody>
                `;

                data.lessons.filter(l => new Date(l.StartDateTime) > new Date()).slice(0, 5).forEach(lesson => {
                    const dateTime = new Date(lesson.StartDateTime);
                    tableHtml += `
                        <tr>
                            <td>${dateTime.toLocaleString()}</td>
                            <td>${lesson.Student.Name}</td>
                            <td>${lesson.Instrument}</td>
                            <td>
                                ${lesson.Mode === 'Virtual' ?
                                    '<span class="badge bg-info">Virtual</span>' :
                                    '<span class="badge bg-success">In-Person</span>'}
                            </td>
                            <td>${lesson.Duration} minutes</td>
                            <td>$${lesson.Price.toFixed(2)}</td>
                        </tr>
                    `;
                });

                tableHtml += '</tbody></table>';
                document.getElementById('upcomingLessons').innerHTML = tableHtml;
            } else {
                document.getElementById('upcomingLessons').innerHTML = `
                    <div class="text-center text-muted py-4">
                        <i class="fas fa-calendar-times fa-3x mb-3"></i>
                        <h5>No upcoming lessons</h5>
                        <p>You haven't scheduled any lessons yet.</p>
                    </div>
                `;
            }
        }
    } catch (error) {
        console.error('Error loading upcoming lessons:', error);
        showAlert('Error loading lessons', 'danger');
    }
}

async function loadTeacherProfile() {
    try {
        const response = await fetch(`${API_BASE}/teacher/profile/${currentTeacherId}`);
        const data = await response.json();

        if (data.success && data.profile) {
            const profile = data.profile;

            // Populate form fields
            document.getElementById('userId').value = profile.userId;
            
            // Parse and check instruments (comma-separated string)
            const instruments = profile.instrumentTaught ? profile.instrumentTaught.split(',').map(i => i.trim()) : [];
            document.querySelectorAll('.instrument-checkbox').forEach(checkbox => {
                checkbox.checked = instruments.includes(checkbox.value);
            });
            
            document.getElementById('customLessonRate').value = profile.customLessonRate || '';
            document.getElementById('bio').value = profile.bio || '';

            // Update current settings display
            document.getElementById('currentInstrument').textContent = profile.instrumentTaught || 'None selected';
            document.getElementById('currentRate').textContent = profile.effectiveRate.toFixed(2);
            document.getElementById('currentBio').textContent = profile.bio || 'No bio provided';
        }
    } catch (error) {
        console.error('Error loading teacher profile:', error);
        showAlert('Error loading profile', 'danger');
    }
}

document.getElementById('profileForm')?.addEventListener('submit', async function(e) {
    e.preventDefault();

    const userId = document.getElementById('userId').value;
    
    // Collect all checked instruments
    const instrumentsTaught = Array.from(document.querySelectorAll('.instrument-checkbox:checked'))
        .map(checkbox => checkbox.value);
    
    const customLessonRate = document.getElementById('customLessonRate').value;
    const bio = document.getElementById('bio').value;
    const errorDiv = document.getElementById('profileError');

    if (instrumentsTaught.length === 0) {
        errorDiv.textContent = 'Please select at least one instrument.';
        errorDiv.classList.remove('d-none');
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/teacher/profile/${userId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                instrumentsTaught,
                bio,
                customLessonRate: customLessonRate ? parseFloat(customLessonRate) : null
            })
        });

        const result = await response.json();

        if (result.success) {
            showAlert('Profile updated successfully!', 'success');
            errorDiv.classList.add('d-none');

            // Reload profile to show updated info
            loadTeacherProfile();
        } else {
            errorDiv.textContent = result.message;
            errorDiv.classList.remove('d-none');
        }
    } catch (error) {
        errorDiv.textContent = 'An error occurred while updating the profile. Please try again.';
        errorDiv.classList.remove('d-none');
    }
});

async function loadAvailabilityList() {
    console.log('=== Loading Teacher Availability ===');
    console.log('Teacher ID:', currentTeacherId);
    console.log('Timestamp:', new Date().toISOString());
    
    try {
        const data = await apiCall(`/teacher/availability/${currentTeacherId}`);

        if (data.success && data.availabilities) {
            if (data.availabilities.length > 0) {
                let tableHtml = `
                    <table class="table table-hover">
                        <thead>
                            <tr>
                                <th>Date & Time</th>
                                <th>Duration</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                `;

                data.availabilities.forEach(availability => {
                    const dateTime = new Date(availability.StartDateTime);
                    tableHtml += `
                        <tr>
                            <td>${dateTime.toLocaleString()}</td>
                            <td>${availability.Duration} minutes</td>
                            <td>
                                <button class="btn btn-sm btn-outline-danger" onclick="removeAvailability(${availability.AvailabilityId})">
                                    <i class="fas fa-trash me-1"></i>Remove
                                </button>
                            </td>
                        </tr>
                    `;
                });

                tableHtml += '</tbody></table>';
                document.getElementById('availabilityList').innerHTML = tableHtml;
            } else {
                document.getElementById('availabilityList').innerHTML = `
                    <div class="text-center text-muted py-4">
                        <i class="fas fa-calendar-plus fa-3x mb-3"></i>
                        <h5>No availability slots</h5>
                        <p>Add some time slots for students to book.</p>
                    </div>
                `;
            }
        }
    } catch (error) {
        console.error('Error loading availability list:', error);
        showAlert('Error loading availability', 'danger');
    }
}

document.getElementById('availabilityForm')?.addEventListener('submit', async function(e) {
    e.preventDefault();

    const availabilityDate = document.getElementById('availabilityDate').value;
    const availabilityTime = document.getElementById('availabilityTime').value;
    const duration = document.getElementById('duration').value;
    const errorDiv = document.getElementById('availabilityError');

    if (!availabilityDate || !availabilityTime) {
        errorDiv.textContent = 'Please select a date and time.';
        errorDiv.classList.remove('d-none');
        return;
    }

    // Combine date and time into ISO format
    const startDateTime = new Date(`${availabilityDate}T${availabilityTime}:00`);
    
    // Check if time is in the past
    if (startDateTime <= new Date()) {
        errorDiv.textContent = 'Cannot schedule availability in the past.';
        errorDiv.classList.remove('d-none');
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/teacher/availability`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                teacherId: currentTeacherId,
                startDateTime: startDateTime.toISOString(),
                duration: parseInt(duration)
            })
        });

        const result = await response.json();

        if (result.success) {
            showAlert('Availability added successfully!', 'success');
            errorDiv.classList.add('d-none');

            // Reset form and reload list
            document.getElementById('availabilityForm').reset();
            loadAvailabilityList();
        } else {
            errorDiv.textContent = result.message;
            errorDiv.classList.remove('d-none');
        }
    } catch (error) {
        errorDiv.textContent = 'An error occurred while adding availability. Please try again.';
        errorDiv.classList.remove('d-none');
    }
});

async function removeAvailability(availabilityId) {
    if (!confirm('Are you sure you want to remove this availability slot?')) {
        return;
    }

    console.log('=== Removing Availability ===');
    console.log('Availability ID:', availabilityId);
    console.log('Teacher ID:', currentTeacherId);
    console.log('Timestamp:', new Date().toISOString());

    try {
        const result = await apiCall(`/teacher/availability/${availabilityId}?teacherId=${currentTeacherId}`, {
            method: 'DELETE'
        });

        if (result.success) {
            console.log('Availability removed successfully');
            showAlert('Availability removed successfully!', 'success');
            loadAvailabilityList();
        } else {
            showAlert(`${result.message}${result.correlationId ? ' (ID: ' + result.correlationId + ')' : ''}`, 'danger');
        }
    } catch (error) {
        console.error('Error removing availability:', error);
        showAlert(`Error removing availability: ${error.message || 'Unknown error'}${error.correlationId ? ' (ID: ' + error.correlationId + ')' : ''}`, 'danger');
    }
}

async function loadLessonsTable() {
    try {
        const response = await fetch(`${API_BASE}/teacher/lessons/${currentTeacherId}`);
        const data = await response.json();

        if (data.success && data.lessons) {
            if (data.lessons.length > 0) {
                let tableHtml = `
                    <table class="table table-hover">
                        <thead>
                            <tr>
                                <th>Date & Time</th>
                                <th>Student</th>
                                <th>Instrument</th>
                                <th>Mode</th>
                                <th>Duration</th>
                                <th>Price</th>
                                <th>Status</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                `;

                data.lessons.forEach(lesson => {
                    const dateTime = new Date(lesson.StartDateTime);
                    const isPast = dateTime < new Date();
                    const canCancel = lesson.Status === 'Scheduled' && !isPast;

                    let statusBadge = '';
                    if (lesson.Status === 'Scheduled') {
                        statusBadge = '<span class="badge bg-success">Scheduled</span>';
                    } else if (lesson.Status === 'Completed') {
                        statusBadge = '<span class="badge bg-info">Completed</span>';
                    } else if (lesson.Status === 'Cancelled') {
                        statusBadge = '<span class="badge bg-danger">Cancelled</span>';
                    }

                    tableHtml += `
                        <tr>
                            <td>${dateTime.toLocaleString()}</td>
                            <td>${lesson.Student.Name}</td>
                            <td>${lesson.Instrument}</td>
                            <td>
                                ${lesson.Mode === 'Virtual' ?
                                    '<span class="badge bg-info">Virtual</span>' :
                                    '<span class="badge bg-success">In-Person</span>'}
                            </td>
                            <td>${lesson.Duration} minutes</td>
                            <td>$${lesson.Price.toFixed(2)}</td>
                            <td>${statusBadge}</td>
                            <td>
                                ${canCancel ? `
                                    <button class="btn btn-sm btn-danger" onclick="cancelTeacherLesson(${lesson.LessonId})">
                                        <i class="fas fa-times me-1"></i>Cancel
                                    </button>
                                ` : '-'}
                            </td>
                        </tr>
                    `;
                });

                tableHtml += '</tbody></table>';
                document.getElementById('lessonsTable').innerHTML = tableHtml;
            } else {
                document.getElementById('lessonsTable').innerHTML = `
                    <div class="text-center text-muted py-4">
                        <i class="fas fa-calendar-times fa-3x mb-3"></i>
                        <h5>No lessons found</h5>
                        <p>You haven't scheduled any lessons yet.</p>
                    </div>
                `;
            }
        }
    } catch (error) {
        console.error('Error loading lessons table:', error);
        showAlert('Error loading lessons', 'danger');
    }
}

function filterLessons() {
    const dateFilter = document.getElementById('lessonDateFilter').value;
    if (dateFilter) {
        // In a real implementation, this would filter the lessons by date
        // For now, just reload all lessons
        loadLessonsTable();
    }
}

function clearLessonFilter() {
    document.getElementById('lessonDateFilter').value = '';
    loadLessonsTable();
}

// Navigation functions
function showTeacherDashboard() {
    hideAllSections();
    document.getElementById('teacherDashboard').style.display = 'block';
    loadTeacherDashboard();
}

function showTeacherProfile() {
    hideAllSections();
    document.getElementById('teacherProfile').style.display = 'block';
    loadTeacherProfile();
}

function showTeacherAvailability() {
    hideAllSections();
    document.getElementById('teacherAvailability').style.display = 'block';
    loadAvailabilityList();
}

function showTeacherLessons() {
    hideAllSections();
    document.getElementById('teacherLessons').style.display = 'block';
    loadLessonsTable();
}

function hideAllSections() {
    const sections = ['teacherDashboard', 'teacherProfile', 'teacherAvailability', 'teacherLessons'];
    sections.forEach(section => {
        document.getElementById(section).style.display = 'none';
    });
}

function showAlert(message, type = 'info') {
    const alertContainer = document.getElementById('alertContainer');
    const alertHtml = `
        <div class="alert alert-${type} alert-dismissible fade show" role="alert">
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    alertContainer.innerHTML = alertHtml;

    // Auto-dismiss after 5 seconds
    setTimeout(() => {
        const alert = alertContainer.querySelector('.alert');
        if (alert) {
            alert.remove();
        }
    }, 5000);
}

// Lesson Cancellation
async function cancelTeacherLesson(lessonId) {
    if (!confirm('Are you sure you want to cancel this lesson? The student will be notified.')) {
        return;
    }

    try {
        const teacherId = currentTeacherId || sessionStorage.getItem('userId');
        const response = await fetch(`${API_BASE}/teacher/lesson/${lessonId}?teacherId=${teacherId}`, {
            method: 'DELETE'
        });

        const data = await response.json();

        if (data.success) {
            showAlert('Lesson cancelled successfully', 'success');
            await loadLessonsTable();
        } else {
            showAlert(data.message || 'Failed to cancel lesson', 'danger');
        }
    } catch (error) {
        console.error('Error cancelling lesson:', error);
        showAlert('Error cancelling lesson', 'danger');
    }
}
