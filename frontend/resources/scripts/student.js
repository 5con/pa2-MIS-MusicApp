// Student Dashboard JavaScript
let availabilityCalendar = null;
let selectedTeacherId = null;
let currentViewMode = 'table'; // 'table' or 'calendar'

document.addEventListener('DOMContentLoaded', function() {
    if (window.location.pathname.includes('student.html')) {
        loadStudentDashboard();
        loadTeachers();
        setupViewToggle();
        initializeAvailabilityCalendar();
    }
});

function loadStudentDashboard() {
    // Load upcoming lessons for student
    loadUpcomingLessons();
}

async function loadUpcomingLessons() {
    try {
        // For now, we'll show a placeholder since we don't have student-specific lesson API
        // In a real app, this would fetch from /Student/GetLessons
        const upcomingLessonsHtml = `
            <table class="table table-hover">
                <thead>
                    <tr>
                        <th>Date & Time</th>
                        <th>Teacher</th>
                        <th>Instrument</th>
                        <th>Mode</th>
                        <th>Price</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td colspan="5" class="text-center text-muted">
                            <i class="fas fa-info-circle me-2"></i>
                            No upcoming lessons. <a href="#" onclick="showBookLesson()">Book your first lesson!</a>
                        </td>
                    </tr>
                </tbody>
            </table>
        `;
        document.getElementById('upcomingLessons').innerHTML = upcomingLessonsHtml;
    } catch (error) {
        console.error('Error loading upcoming lessons:', error);
    }
}

async function loadTeachers() {
    console.log('=== Loading Teachers ===');
    console.log('Timestamp:', new Date().toISOString());
    
    try {
        const data = await apiCall('/student/availabilities');

        if (data.success && data.availabilitiesByTeacher) {
            const teacherSelect = document.getElementById('teacherSelect');
            teacherSelect.innerHTML = '<option value="">Choose a teacher...</option>';

            data.availabilitiesByTeacher.forEach(teacherGroup => {
                const option = document.createElement('option');
                option.value = teacherGroup.Teacher.UserId;
                option.textContent = `${teacherGroup.Teacher.Name} (${teacherGroup.Teacher.InstrumentTaught})`;
                teacherSelect.appendChild(option);
            });

            console.log('Teachers loaded successfully:', data.availabilitiesByTeacher.length);
        }
    } catch (error) {
        console.error('Error loading teachers:', error);
        showAlert(`Error loading teachers: ${error.message || 'Unknown error'}${error.correlationId ? ' (ID: ' + error.correlationId + ')' : ''}`, 'danger');
    }
}

function loadTeacherAvailability() {
    selectedTeacherId = document.getElementById('teacherSelect').value;

    if (!selectedTeacherId) {
        showAlert('Please select a teacher first', 'warning');
        return;
    }

    // Update teacher info display
    const teacherSelect = document.getElementById('teacherSelect');
    const selectedOption = teacherSelect.options[teacherSelect.selectedIndex];
    document.getElementById('selectedTeacherName').textContent = selectedOption.textContent.split(' (')[0];
    document.getElementById('selectedTeacherInstrument').textContent = selectedOption.textContent.split(' (')[1].replace(')', '');
    document.getElementById('selectedTeacherInfo').style.display = 'block';

    if (currentViewMode === 'table') {
        loadAvailabilityTable();
    } else {
        loadAvailabilityCalendar();
    }
}

async function loadAvailabilityTable() {
    console.log('=== Loading Availability Table ===');
    console.log('Teacher ID:', selectedTeacherId);
    console.log('Timestamp:', new Date().toISOString());
    
    try {
        const data = await apiCall(`/student/availabilities?teacherId=${selectedTeacherId}`);

        if (data.success && data.availabilitiesByTeacher) {
            const teacherGroup = data.availabilitiesByTeacher.find(t => t.Teacher.UserId == selectedTeacherId);

            if (teacherGroup && teacherGroup.Availabilities.length > 0) {
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

                teacherGroup.Availabilities.forEach(availability => {
                    const dateTime = new Date(availability.StartDateTime);
                    tableHtml += `
                        <tr>
                            <td>${dateTime.toLocaleString()}</td>
                            <td>${availability.Duration} minutes</td>
                            <td>
                                <button class="btn btn-sm btn-primary" onclick="showBookingModal(${availability.AvailabilityId}, '${dateTime.toLocaleString()}', '${teacherGroup.Teacher.Name}', '${teacherGroup.Teacher.InstrumentTaught}', ${availability.Duration}, 30)">
                                    <i class="fas fa-calendar-plus me-1"></i>Book
                                </button>
                            </td>
                        </tr>
                    `;
                });

                tableHtml += '</tbody></table>';
                document.getElementById('availabilityTable').innerHTML = tableHtml;
            } else {
                document.getElementById('availabilityTable').innerHTML = `
                    <div class="text-center text-muted py-4">
                        <i class="fas fa-calendar-times fa-3x mb-3"></i>
                        <h5>No availability slots found</h5>
                        <p>This teacher has no available time slots.</p>
                    </div>
                `;
            }
        }
    } catch (error) {
        console.error('Error loading availability table:', error);
        showAlert('Error loading availability', 'danger');
    }
}

function initializeAvailabilityCalendar() {
    const calendarEl = document.getElementById('availabilityCalendar');

    if (!calendarEl) return;
    if (typeof window.FullCalendar === 'undefined') { console.warn('FullCalendar not loaded'); return; }

    availabilityCalendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridDay',
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: ''
        },
        height: 600,
        events: function(info, successCallback, failureCallback) {
            if (selectedTeacherId) {
                loadAvailabilityCalendarEvents(info.start, info.end, successCallback, failureCallback);
            } else {
                successCallback([]);
            }
        },
        eventClick: function(info) {
            showBookingModalFromCalendar(info.event);
        }
    });

    availabilityCalendar.render();
}

async function loadAvailabilityCalendarEvents(start, end, successCallback, failureCallback) {
    try {
        const response = await fetch(`${API_BASE}/student/calendar/${selectedTeacherId}?startDate=${start.toISOString()}&endDate=${end.toISOString()}`);
        const data = await response.json();

        if (data.success) {
            successCallback(data.events);
        } else {
            failureCallback(new Error(data.message));
        }
    } catch (error) {
        console.error('Error loading calendar events:', error);
        failureCallback(error);
    }
}

function setupViewToggle() {
    const calendarToggle = document.getElementById('calendarViewToggle');
    const tableView = document.getElementById('tableView');
    const calendarView = document.getElementById('calendarView');

    if (calendarToggle) {
        calendarToggle.addEventListener('change', function() {
            if (this.checked) {
                currentViewMode = 'calendar';
                tableView.style.display = 'none';
                calendarView.style.display = 'block';

                if (selectedTeacherId) {
                    loadAvailabilityCalendar();
                }
            } else {
                currentViewMode = 'table';
                tableView.style.display = 'block';
                calendarView.style.display = 'none';

                if (selectedTeacherId) {
                    loadAvailabilityTable();
                }
            }
        });
    }
}

function showBookingModal(availabilityId, dateTime, teacher, instrument, duration, price) {
    document.getElementById('bookingAvailabilityId').value = availabilityId;
    document.getElementById('bookingDateTime').textContent = dateTime;
    document.getElementById('bookingTeacher').textContent = teacher;
    document.getElementById('bookingInstrument').textContent = instrument;
    document.getElementById('bookingDuration').textContent = `${duration} minutes`;
    document.getElementById('bookingPrice').textContent = `$${price.toFixed(2)}`;
    document.getElementById('lessonMode').value = 'Virtual';
    document.getElementById('bookingError').classList.add('d-none');

    // Reset sheet music upload
    document.getElementById('sheetMusicFile').value = '';
    document.getElementById('uploadedFileName').style.display = 'none';
    uploadedSheetMusicPath = null;

    // Reset recurring options
    document.getElementById('recurringLesson').checked = false;
    document.getElementById('recurringOptions').style.display = 'none';
    document.getElementById('occurrences').value = 4;
    document.getElementById('intervalWeeks').value = 1;

    new bootstrap.Modal(document.getElementById('bookingModal')).show();
}

function showBookingModalFromCalendar(event) {
    const availabilityId = event.extendedProps.availabilityId;
    const startDate = new Date(event.start);
    const teacherName = event.title.includes('Available') ? 'Selected Teacher' : 'Teacher';
    const duration = event.extendedProps.duration;
    const price = 30; // Default price

    showBookingModal(availabilityId, startDate.toLocaleString(), teacherName, 'Instrument', duration, price);
}

async function confirmBooking() {
    const availabilityId = document.getElementById('bookingAvailabilityId').value;
    const lessonMode = document.getElementById('lessonMode').value;
    const isRecurring = document.getElementById('recurringLesson').checked;
    const errorDiv = document.getElementById('bookingError');

    if (!availabilityId) {
        errorDiv.textContent = 'No availability selected.';
        errorDiv.classList.remove('d-none');
        return;
    }

    // Upload sheet music if selected
    const fileInput = document.getElementById('sheetMusicFile');
    if (fileInput.files.length > 0) {
        showAlert('Uploading sheet music...', 'info');
        uploadedSheetMusicPath = await uploadSheetMusic();
        if (!uploadedSheetMusicPath) {
            return; // Upload failed, don't proceed
        }
    }

    // Store booking data for payment processing
    window.currentBookingData = {
        availabilityId: parseInt(availabilityId),
        lessonMode,
        isRecurring,
        sheetMusicPath: uploadedSheetMusicPath
    };

    if (isRecurring) {
        const occurrences = parseInt(document.getElementById('occurrences').value);
        const intervalWeeks = parseInt(document.getElementById('intervalWeeks').value);

        if (occurrences < 2 || occurrences > 52) {
            errorDiv.textContent = 'Number of lessons must be between 2 and 52.';
            errorDiv.classList.remove('d-none');
            return;
        }

        window.currentBookingData.occurrences = occurrences;
        window.currentBookingData.intervalWeeks = intervalWeeks;
    }

    // Show payment modal
    showPaymentModal();
}

async function showPaymentModal() {
    // Create payment modal
    const paymentModalHtml = `
        <div class="modal fade" id="paymentModal" tabindex="-1">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Payment Information</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <form id="paymentForm">
                            <div class="mb-3">
                                <label for="cardNumber" class="form-label">Card Number</label>
                                <input type="text" class="form-control" id="cardNumber" placeholder="1234 5678 9012 3456" required>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="mb-3">
                                        <label for="expiryDate" class="form-label">Expiry Date</label>
                                        <input type="text" class="form-control" id="expiryDate" placeholder="MM/YY" required>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="mb-3">
                                        <label for="cvv" class="form-label">CVV</label>
                                        <input type="text" class="form-control" id="cvv" placeholder="123" required>
                                    </div>
                                </div>
                            </div>
                            <div id="paymentError" class="alert alert-danger d-none"></div>
                        </form>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn btn-primary" onclick="processPayment()">Pay & Book</button>
                    </div>
                </div>
            </div>
        </div>
    `;

    // Remove existing modal if it exists
    const existingModal = document.getElementById('paymentModal');
    if (existingModal) {
        existingModal.remove();
    }

    // Add modal to page
    document.body.insertAdjacentHTML('beforeend', paymentModalHtml);

    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('paymentModal'));
    modal.show();

    // Clean up modal when hidden
    document.getElementById('paymentModal').addEventListener('hidden.bs.modal', function() {
        this.remove();
    });
}

async function processPayment() {
    const cardNumber = document.getElementById('cardNumber').value;
    const expiryDate = document.getElementById('expiryDate').value;
    const cvv = document.getElementById('cvv').value;
    const errorDiv = document.getElementById('paymentError');

    console.log('=== Processing Payment ===');
    console.log('Timestamp:', new Date().toISOString());

    // Basic client-side validation
    if (!cardNumber || !expiryDate || !cvv) {
        const error = { message: 'Please fill in all payment fields.', errorCode: 'VALIDATION_ERROR' };
        displayError(errorDiv, error);
        console.warn('Payment validation failed: Missing fields');
        return;
    }

    try {
        // Validate payment first
        console.log('Validating payment...');
        const paymentResult = await apiCall('/student/validate-payment', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                cardNumber,
                expiryDate,
                cvv
            })
        });

        if (!paymentResult.success) {
            displayError(errorDiv, { message: paymentResult.message, errorCode: 'PAYMENT_VALIDATION_FAILED' });
            return;
        }

        console.log('Payment validated successfully');

        // Get booking data
        const bookingData = window.currentBookingData;
        const studentId = currentStudentId || sessionStorage.getItem('userId') || 1;

        console.log('Booking lesson - Data:', bookingData);

        let bookingResult;

        if (bookingData.isRecurring) {
            // Book recurring lessons
            bookingResult = await apiCall('/student/book-recurring', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    availabilityId: bookingData.availabilityId,
                    studentId: studentId,
                    mode: bookingData.lessonMode,
                    occurrences: bookingData.occurrences,
                    intervalWeeks: bookingData.intervalWeeks
                })
            });
        } else {
            // Book single lesson
            bookingResult = await apiCall('/student/book', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    availabilityId: bookingData.availabilityId,
                    studentId: studentId,
                    mode: bookingData.lessonMode,
                    sheetMusicPath: bookingData.sheetMusicPath
                })
            });
        }

        if (bookingResult.success) {
            console.log('Lesson booked successfully!');
            
            bootstrap.Modal.getInstance(document.getElementById('paymentModal')).hide();
            bootstrap.Modal.getInstance(document.getElementById('bookingModal')).hide();
            
            if (bookingData.isRecurring) {
                showAlert(`${bookingData.occurrences} recurring lessons booked successfully!`, 'success');
            } else {
                showAlert('Lesson booked successfully!', 'success');
            }

            // Reset form
            document.getElementById('sheetMusicFile').value = '';
            document.getElementById('uploadedFileName').style.display = 'none';
            document.getElementById('recurringLesson').checked = false;
            document.getElementById('recurringOptions').style.display = 'none';
            uploadedSheetMusicPath = null;

            // Refresh availability if currently viewing
            if (selectedTeacherId) {
                if (currentViewMode === 'table') {
                    loadAvailabilityTable();
                } else {
                    loadAvailabilityCalendar();
                }
            }
        } else {
            displayError(errorDiv, { message: bookingResult.message, errorCode: 'BOOKING_FAILED' });
        }
    } catch (error) {
        console.error('Payment/Booking error:', error);
        displayError(errorDiv, error);
    }
}

function showBookLesson() {
    document.getElementById('studentDashboard').style.display = 'none';
    document.getElementById('bookLessonSection').style.display = 'block';

    // Reset form
    document.getElementById('teacherSelect').value = '';
    document.getElementById('selectedTeacherInfo').style.display = 'none';
    document.getElementById('availabilityTable').innerHTML = '';
    document.getElementById('calendarViewToggle').checked = false;
    document.getElementById('tableView').style.display = 'block';
    document.getElementById('calendarView').style.display = 'none';
    currentViewMode = 'table';
}

function showStudentDashboard() {
    document.getElementById('studentDashboard').style.display = 'block';
    document.getElementById('bookLessonSection').style.display = 'none';
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

// My Lessons Section
let currentLessonFilter = 'all';
let allStudentLessons = [];

async function showMyLessons() {
    document.getElementById('studentDashboard').style.display = 'none';
    document.getElementById('myLessonsSection').style.display = 'block';
    document.getElementById('bookLessonSection').style.display = 'none';

    await loadMyLessons();
}

async function loadMyLessons(status = null) {
    try {
        const studentId = currentStudentId || sessionStorage.getItem('userId');
        if (!studentId) {
            showAlert('Please log in to view your lessons', 'warning');
            return;
        }

        const url = status ? `/student/lessons/${studentId}?status=${status}` : `/student/lessons/${studentId}`;
        const data = await apiCall(url);

        if (data.success) {
            allStudentLessons = data.lessons;
            displayMyLessons(data.lessons);
        }
    } catch (error) {
        console.error('Error loading lessons:', error);
        showAlert('Error loading lessons', 'danger');
    }
}

function displayMyLessons(lessons) {
    const tableContainer = document.getElementById('myLessonsTable');

    if (!lessons || lessons.length === 0) {
        tableContainer.innerHTML = `
            <div class="text-center text-muted py-4">
                <i class="fas fa-calendar-times fa-3x mb-3"></i>
                <h5>No lessons found</h5>
                <p>Book your first lesson to get started!</p>
            </div>
        `;
        return;
    }

    let tableHtml = `
        <table class="table table-hover">
            <thead>
                <tr>
                    <th>Date & Time</th>
                    <th>Teacher</th>
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

    lessons.forEach(lesson => {
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
                <td>${lesson.Teacher.Name}</td>
                <td>${lesson.Instrument}</td>
                <td>${lesson.Mode}</td>
                <td>${lesson.Duration} min</td>
                <td>$${lesson.Price.toFixed(2)}</td>
                <td>${statusBadge}</td>
                <td>
                    ${canCancel ? `
                        <button class="btn btn-sm btn-danger" onclick="cancelMyLesson(${lesson.LessonId})">
                            <i class="fas fa-times me-1"></i>Cancel
                        </button>
                    ` : '-'}
                </td>
            </tr>
        `;
    });

    tableHtml += '</tbody></table>';
    tableContainer.innerHTML = tableHtml;
}

function filterMyLessons(filter) {
    currentLessonFilter = filter;

    // Update active tab
    const tabs = document.querySelectorAll('#lessonFilterTabs .nav-link');
    tabs.forEach(tab => tab.classList.remove('active'));
    event.target.classList.add('active');

    if (filter === 'all') {
        displayMyLessons(allStudentLessons);
    } else {
        const filtered = allStudentLessons.filter(l => l.Status === filter);
        displayMyLessons(filtered);
    }
}

async function cancelMyLesson(lessonId) {
    if (!confirm('Are you sure you want to cancel this lesson?')) {
        return;
    }

    try {
        const studentId = currentStudentId || sessionStorage.getItem('userId');
        const data = await apiCall(`/student/lesson/${lessonId}?studentId=${studentId}`, {
            method: 'DELETE'
        });

        if (data.success) {
            showAlert('Lesson cancelled successfully', 'success');
            await loadMyLessons();
        } else {
            showAlert(data.message || 'Failed to cancel lesson', 'danger');
        }
    } catch (error) {
        console.error('Error cancelling lesson:', error);
        showAlert('Error cancelling lesson', 'danger');
    }
}

// Instrument Search
async function searchByInstrument() {
    const instrument = document.getElementById('instrumentSearch').value.trim();

    if (!instrument) {
        showAlert('Please enter an instrument name', 'warning');
        return;
    }

    try {
        const data = await apiCall(`/student/search?instrument=${encodeURIComponent(instrument)}`);

        if (data.success && data.results) {
            displaySearchResults(data.results);
        }
    } catch (error) {
        console.error('Error searching by instrument:', error);
        showAlert('Error searching for teachers', 'danger');
    }
}

function displaySearchResults(results) {
    const searchResults = document.getElementById('searchResults');

    if (!results || results.length === 0) {
        searchResults.innerHTML = `
            <div class="alert alert-info">
                <i class="fas fa-info-circle me-2"></i>
                No teachers found for this instrument. Try a different search.
            </div>
        `;
        searchResults.style.display = 'block';
        return;
    }

    let html = '<h6 class="mt-3">Search Results:</h6><div class="list-group">';

    results.forEach(result => {
        const teacher = result.Teacher;
        const availabilityCount = result.Availabilities.length;

        html += `
            <div class="list-group-item">
                <div class="d-flex justify-content-between align-items-center">
                    <div>
                        <h6 class="mb-1">${teacher.Name}</h6>
                        <p class="mb-1 text-muted">${teacher.Email}</p>
                        <small class="text-success">${availabilityCount} available time slot${availabilityCount !== 1 ? 's' : ''}</small>
                    </div>
                    <button class="btn btn-sm btn-primary" onclick="selectTeacherFromSearch(${teacher.UserId})">
                        <i class="fas fa-arrow-right me-1"></i>View
                    </button>
                </div>
            </div>
        `;
    });

    html += '</div>';
    searchResults.innerHTML = html;
    searchResults.style.display = 'block';
}

function selectTeacherFromSearch(teacherId) {
    // Select the teacher in the dropdown
    document.getElementById('teacherSelect').value = teacherId;
    selectedTeacherId = teacherId;

    // Clear search
    clearInstrumentSearch();

    // Load availability
    loadTeacherAvailability();

    // Scroll to availability section
    document.getElementById('selectedTeacherInfo').scrollIntoView({ behavior: 'smooth' });
}

function clearInstrumentSearch() {
    document.getElementById('instrumentSearch').value = '';
    document.getElementById('searchResults').style.display = 'none';
    document.getElementById('searchResults').innerHTML = '';
}

// Recurring Lesson Options
function toggleRecurringOptions() {
    const isRecurring = document.getElementById('recurringLesson').checked;
    document.getElementById('recurringOptions').style.display = isRecurring ? 'block' : 'none';
}

// Sheet Music Upload
let uploadedSheetMusicPath = null;

async function uploadSheetMusic() {
    const fileInput = document.getElementById('sheetMusicFile');
    const file = fileInput.files[0];

    if (!file) {
        return null;
    }

    try {
        const formData = new FormData();
        formData.append('file', file);

        const response = await fetch(`${API_BASE}/student/sheet-music`, {
            method: 'POST',
            body: formData
        });

        const data = await response.json();

        if (data.success) {
            uploadedSheetMusicPath = data.filePath;
            document.getElementById('fileName').textContent = file.name;
            document.getElementById('uploadedFileName').style.display = 'block';
            return data.filePath;
        } else {
            showAlert('Failed to upload sheet music: ' + data.message, 'danger');
            return null;
        }
    } catch (error) {
        console.error('Error uploading sheet music:', error);
        showAlert('Error uploading sheet music', 'danger');
        return null;
    }
}