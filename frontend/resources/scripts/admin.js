// Admin Dashboard JavaScript
let calendar = null;
let currentCalendarView = 'dayGridDay';
let quarterlyRevenueChart = null;

document.addEventListener('DOMContentLoaded', function() {
    loadAdminDashboard();
    initializeCalendar();
    setupCalendarViewToggle();
});

function loadAdminDashboard() {
    // Load dashboard stats
    loadDashboardStats();
}

async function loadDashboardStats() {
    try {
        // For now, we'll use mock data since we don't have a proper API endpoint
        // In a real application, this would fetch from the backend

        // Mock data - in real app this would come from API
        document.getElementById('totalLessons').textContent = '24';
        document.getElementById('totalTeachers').textContent = '8';
        document.getElementById('totalStudents').textContent = '16';
        document.getElementById('repeatStudents').textContent = '12';
        document.getElementById('totalStudentsLessons').textContent = '16';
        document.getElementById('repeatRate').textContent = '75%';

        // Load real data from API when available
        await loadReportsData();

    } catch (error) {
        console.error('Error loading dashboard stats:', error);
        showAlert('Error loading dashboard data', 'danger');
    }
}

async function loadReportsData() {
    console.log('=== Loading Admin Reports ===');
    console.log('Timestamp:', new Date().toISOString());
    
    try {
        const data = await apiCall('/admin/reports');

        if (data.success) {
            // Update user metrics
            if (data.userMetrics) {
                document.getElementById('totalLessons').textContent = data.userMetrics.totalUsers || '0';
                document.getElementById('totalTeachers').textContent = data.userMetrics.totalTeachers || '0';
                document.getElementById('totalStudents').textContent = data.userMetrics.totalStudents || '0';
            }

            // Update repeat booking rate
            if (data.repeatBookingRate) {
                document.getElementById('repeatStudents').textContent = data.repeatBookingRate.studentsWithMultipleLessons || '0';
                document.getElementById('totalStudentsLessons').textContent = data.repeatBookingRate.totalStudentsWithLessons || '0';
                document.getElementById('repeatRate').textContent = `${data.repeatBookingRate.repeatRate || '0'}%`;
            }
        }
    } catch (error) {
        console.error('Error loading reports data:', error);
    }
}

function initializeCalendar() {
    const calendarEl = document.getElementById('calendar');

    calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridDay',
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: ''
        },
        height: 600,
        events: function(info, successCallback, failureCallback) {
            loadCalendarEvents(info.start, info.end, successCallback, failureCallback);
        },
        eventClick: function(info) {
            showLessonDetails(info.event);
        },
        eventDisplay: 'block'
    });

    calendar.render();
}

async function loadCalendarEvents(start, end, successCallback, failureCallback) {
    try {
        const response = await fetch(`${API_BASE}/admin/calendar-events?startDate=${start.toISOString()}&endDate=${end.toISOString()}`);
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

function setupCalendarViewToggle() {
    const viewRadios = document.querySelectorAll('input[name="calendarView"]');

    viewRadios.forEach(radio => {
        radio.addEventListener('change', function() {
            if (this.checked) {
                switch (this.id) {
                    case 'dayView':
                        calendar.changeView('dayGridDay');
                        currentCalendarView = 'dayGridDay';
                        break;
                    case 'weekView':
                        calendar.changeView('timeGridWeek');
                        currentCalendarView = 'timeGridWeek';
                        break;
                    case 'monthView':
                        calendar.changeView('dayGridMonth');
                        currentCalendarView = 'dayGridMonth';
                        break;
                }
            }
        });
    });
}

function showLessonDetails(event) {
    const details = event.extendedProps;
    const lessonDateTime = new Date(event.start);

    const detailsHtml = `
        <div class="row">
            <div class="col-sm-3"><strong>Date & Time:</strong></div>
            <div class="col-sm-9">${lessonDateTime.toLocaleString()}</div>
        </div>
        <div class="row">
            <div class="col-sm-3"><strong>Student:</strong></div>
            <div class="col-sm-9">${event.title.split(' (')[0]}</div>
        </div>
        <div class="row">
            <div class="col-sm-3"><strong>Teacher:</strong></div>
            <div class="col-sm-9">${event.title.split(' with ')[1].replace(')', '')}</div>
        </div>
        <div class="row">
            <div class="col-sm-3"><strong>Mode:</strong></div>
            <div class="col-sm-9">${details.mode}</div>
        </div>
        <div class="row">
            <div class="col-sm-3"><strong>Duration:</strong></div>
            <div class="col-sm-9">${details.duration} minutes</div>
        </div>
        <div class="row">
            <div class="col-sm-3"><strong>Price:</strong></div>
            <div class="col-sm-9">$${details.price.toFixed(2)}</div>
        </div>
        <div class="row">
            <div class="col-sm-3"><strong>Status:</strong></div>
            <div class="col-sm-9">${details.status}</div>
        </div>
    `;

    document.getElementById('lessonDetails').innerHTML = detailsHtml;
    new bootstrap.Modal(document.getElementById('lessonModal')).show();
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

// Navigation functions
function showAdminDashboard() {
    document.getElementById('dashboardView').style.display = 'block';
    document.getElementById('reportsView').style.display = 'none';
    document.getElementById('allLessonsView').style.display = 'none';
    
    // Update active nav link
    updateActiveNav('dashboard');
}

function showAdminReports() {
    document.getElementById('dashboardView').style.display = 'none';
    document.getElementById('reportsView').style.display = 'block';
    document.getElementById('allLessonsView').style.display = 'none';
    
    // Update active nav link
    updateActiveNav('reports');
    
    // Load reports data
    loadAndDisplayReports();
}

function showAllLessons() {
    document.getElementById('dashboardView').style.display = 'none';
    document.getElementById('reportsView').style.display = 'none';
    document.getElementById('allLessonsView').style.display = 'block';
    
    // Update active nav link
    updateActiveNav('lessons');
    
    // Load lessons data
    loadAndDisplayAllLessons();
}

function updateActiveNav(section) {
    const navLinks = document.querySelectorAll('.navbar-nav .nav-link');
    navLinks.forEach(link => link.classList.remove('active'));
    
    if (section === 'dashboard') {
        navLinks[0]?.classList.add('active');
    } else if (section === 'reports') {
        navLinks[1]?.classList.add('active');
    } else if (section === 'lessons') {
        navLinks[2]?.classList.add('active');
    }
}

// Reports View Functions
async function loadAndDisplayReports() {
    try {
        const response = await fetch(`${API_BASE}/admin/reports`);
        const data = await response.json();

        if (data.success) {
            // Populate Quarterly Revenue (1.1.1)
            if (data.quarterlyRevenue) {
                populateQuarterlyRevenue(data.quarterlyRevenue);
            }

            // Populate Referral Breakdown (1.1.2)
            if (data.referralBreakdown) {
                populateReferralBreakdown(data.referralBreakdown);
            }

            // Populate Popular Instruments (1.2)
            if (data.popularInstruments) {
                populatePopularInstruments(data.popularInstruments);
            }
        } else {
            showAlert('Failed to load reports data', 'danger');
        }

        // Load revenue distribution data (1.1.7 & 1.1.8)
        await loadRevenueDistribution();
    } catch (error) {
        console.error('Error loading reports:', error);
        showAlert('Error loading reports data', 'danger');
    }
}

function populateQuarterlyRevenue(data) {
    if (!data || data.length === 0) {
        return;
    }

    // Destroy existing chart if it exists
    if (quarterlyRevenueChart) {
        quarterlyRevenueChart.destroy();
    }

    // Prepare data for Chart.js
    const labels = data.map(item => item.quarter);
    const revenues = data.map(item => item.revenue);
    const lessonCounts = data.map(item => item.lessonCount);

    // Create the bar chart
    const ctx = document.getElementById('quarterlyRevenueChart').getContext('2d');
    quarterlyRevenueChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Revenue ($)',
                data: revenues,
                backgroundColor: 'rgba(54, 162, 235, 0.6)',
                borderColor: 'rgba(54, 162, 235, 1)',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: function(value) {
                            return '$' + value.toFixed(2);
                        }
                    }
                }
            },
            plugins: {
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const index = context.dataIndex;
                            return [
                                `Revenue: $${context.parsed.y.toFixed(2)}`,
                                `Lessons: ${lessonCounts[index]}`
                            ];
                        }
                    }
                },
                legend: {
                    display: true,
                    position: 'top'
                }
            }
        }
    });
}

function populateReferralBreakdown(data) {
    const tbody = document.getElementById('referralBreakdownTable');
    if (!data || data.length === 0) {
        tbody.innerHTML = '<tr><td colspan="3" class="text-center">No data available</td></tr>';
        return;
    }

    const total = data.reduce((sum, item) => sum + item.count, 0);
    const rows = data.map(item => {
        const percentage = total > 0 ? ((item.count / total) * 100).toFixed(1) : 0;
        return `
            <tr>
                <td>${item.referralSource}</td>
                <td>${item.count}</td>
                <td>${percentage}%</td>
            </tr>
        `;
    }).join('');

    tbody.innerHTML = rows;
}

function populatePopularInstruments(data) {
    const tbody = document.getElementById('popularInstrumentsTable');
    if (!data || data.length === 0) {
        tbody.innerHTML = '<tr><td colspan="3" class="text-center">No data available</td></tr>';
        return;
    }

    const rows = data.map((item, index) => `
        <tr>
            <td>${index + 1}</td>
            <td>${item.instrument}</td>
            <td>${item.count}</td>
        </tr>
    `).join('');

    tbody.innerHTML = rows;
}

// All Lessons View Functions
let currentSort = 'date';
let currentFilterBy = '';
let currentFilterValue = '';
let allFiltersData = null;

async function loadAndDisplayAllLessons(sortBy = 'date', filterBy = '', filterValue = '') {
    currentSort = sortBy;
    currentFilterBy = filterBy;
    currentFilterValue = filterValue;

    try {
        const params = new URLSearchParams({
            sortBy: sortBy,
            filterBy: filterBy || '',
            filterValue: filterValue || ''
        });

        const response = await fetch(`${API_BASE}/admin/lessons?${params}`);
        const data = await response.json();

        if (data.success) {
            // Store filter data
            allFiltersData = data.filters;

            // Populate lessons table
            populateAllLessonsTable(data.lessons);

            // Setup filter controls
            if (!filterBy) {
                setupFilterControls();
            }
        } else {
            showAlert('Failed to load lessons data', 'danger');
        }
    } catch (error) {
        console.error('Error loading lessons:', error);
        showAlert('Error loading lessons data', 'danger');
    }
}

function populateAllLessonsTable(lessons) {
    const tbody = document.getElementById('allLessonsTable');
    if (!lessons || lessons.length === 0) {
        tbody.innerHTML = '<tr><td colspan="8" class="text-center">No lessons found</td></tr>';
        return;
    }

    const rows = lessons.map(lesson => {
        const date = new Date(lesson.startDateTime);
        return `
            <tr>
                <td>${lesson.student?.name || 'N/A'}</td>
                <td>${lesson.teacher?.name || 'N/A'}</td>
                <td>${lesson.instrument}</td>
                <td>${date.toLocaleString()}</td>
                <td>${lesson.duration} min</td>
                <td>${lesson.mode}</td>
                <td>$${lesson.price.toFixed(2)}</td>
                <td><span class="badge bg-${getStatusColor(lesson.status)}">${lesson.status}</span></td>
            </tr>
        `;
    }).join('');

    tbody.innerHTML = rows;
}

function getStatusColor(status) {
    switch (status?.toLowerCase()) {
        case 'confirmed':
        case 'completed':
            return 'success';
        case 'pending':
            return 'warning';
        case 'cancelled':
            return 'danger';
        default:
            return 'secondary';
    }
}

function setupFilterControls() {
    const filterBySelect = document.getElementById('filterBySelect');
    const filterValueSelect = document.getElementById('filterValueSelect');
    const applyFilterBtn = document.getElementById('applyFilterBtn');
    const clearFilterBtn = document.getElementById('clearFilterBtn');

    // Filter type change
    filterBySelect.addEventListener('change', function() {
        const filterType = this.value;
        
        if (!filterType) {
            filterValueSelect.disabled = true;
            filterValueSelect.innerHTML = '<option value="">Select filter type first</option>';
            return;
        }

        filterValueSelect.disabled = false;
        
        let options = '<option value="">Select...</option>';
        if (allFiltersData) {
            const values = filterType === 'teacher' ? allFiltersData.teachers :
                          filterType === 'student' ? allFiltersData.students :
                          filterType === 'instrument' ? allFiltersData.instruments : [];
            
            options += values.map(v => `<option value="${v}">${v}</option>`).join('');
        }
        
        filterValueSelect.innerHTML = options;
    });

    // Apply filter
    applyFilterBtn.addEventListener('click', function() {
        const filterBy = filterBySelect.value;
        const filterValue = filterValueSelect.value;
        
        if (filterBy && filterValue) {
            loadAndDisplayAllLessons(currentSort, filterBy, filterValue);
        }
    });

    // Clear filter
    clearFilterBtn.addEventListener('click', function() {
        filterBySelect.value = '';
        filterValueSelect.value = '';
        filterValueSelect.disabled = true;
        filterValueSelect.innerHTML = '<option value="">Select filter type first</option>';
        loadAndDisplayAllLessons(currentSort, '', '');
    });

    // Sorting
    const sortableHeaders = document.querySelectorAll('.sortable');
    sortableHeaders.forEach(header => {
        header.style.cursor = 'pointer';
        header.addEventListener('click', function() {
            const sortField = this.getAttribute('data-sort');
            loadAndDisplayAllLessons(sortField, currentFilterBy, currentFilterValue);
        });
    });
}

window.generateDummyData = async function () {
    const teachers = Number(document.getElementById('seedTeachers').value || 5);
    const students = Number(document.getElementById('seedStudents').value || 20);
    const lessons = Number(document.getElementById('seedLessons').value || 120);
    try {
		let res = await fetch(`${API_BASE}/admin/dummy-data?teachers=${teachers}&students=${students}&lessons=${lessons}`, { method: 'POST' });
		if (!res.ok && (res.status === 404 || res.status === 405)) {
			// fallback for older backend route
			res = await fetch(`${API_BASE}/admin/seed?teachers=${teachers}&students=${students}&lessons=${lessons}`, { method: 'POST' });
		}
		const data = await res.json();
		if (!data.success) throw new Error(data.message || 'Seed failed');
        showAlert(`Seeded ${data.teachers} teachers, ${data.students} students, ${data.lessons} lessons.`, 'success');
        await loadReportsData();
        if (calendar) calendar.refetchEvents();
    } catch (e) {
        console.error(e);
        showAlert('Failed to generate dummy data.', 'danger');
    }
}

window.resetDatabase = async function () {
    if (!confirm('This will remove all non-admin data. Continue?')) return;
    try {
		let res = await fetch(`${API_BASE}/admin/dummy-data?clear=true`, { method: 'POST' });
		if (!res.ok && (res.status === 404 || res.status === 405)) {
			// fallback for older backend route
			res = await fetch(`${API_BASE}/admin/seed?clear=true`, { method: 'POST' });
		}
		const data = await res.json();
        if (!data.success) throw new Error(data.message || 'Reset failed');
        showAlert('Database reset. You can now reseed.', 'warning');
        await loadReportsData();
        if (calendar) calendar.refetchEvents();
    } catch (e) {
        console.error(e);
        showAlert('Failed to reset database.', 'danger');
    }
}

// Revenue Distribution Functions (1.1.7 & 1.1.8)
async function loadRevenueDistribution() {
    try {
        const response = await fetch(`${API_BASE}/admin/revenue-distribution`);
        const data = await response.json();

        if (data.success) {
            // Populate instrument revenue distribution
            if (data.instrumentDistribution) {
                populateInstrumentRevenueDistribution(data.totalRevenue, data.instrumentDistribution);
            }

            // Populate student revenue distribution
            if (data.studentDistribution) {
                populateStudentRevenueDistribution(data.totalRevenue, data.studentDistribution);
            }
        } else {
            showAlert('Failed to load revenue distribution data', 'danger');
        }
    } catch (error) {
        console.error('Error loading revenue distribution:', error);
        showAlert('Error loading revenue distribution data', 'danger');
    }
}

function populateInstrumentRevenueDistribution(totalRevenue, distributionData) {
    // Update summary stats
    document.getElementById('totalRevenue').textContent = totalRevenue.toFixed(2);
    document.getElementById('instrumentsCount50').textContent = distributionData.instrumentsCount;
    document.getElementById('instrumentsRevenue50').textContent = distributionData.instrumentsRevenue.toFixed(2);

    // Populate table
    const tbody = document.getElementById('instrumentRevenueTable');
    if (!distributionData.instruments || distributionData.instruments.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" class="text-center">No data available</td></tr>';
        return;
    }

    const instruments50Set = new Set(distributionData.instrumentsFor50Percent);
    const rows = distributionData.instruments.map((item, index) => {
        const percentage = totalRevenue > 0 ? (item.Revenue / totalRevenue * 100).toFixed(1) : 0;
        const isInTop50 = instruments50Set.has(item.Instrument);
        const rowClass = isInTop50 ? 'table-success' : '';
        
        return `
            <tr class="${rowClass}">
                <td>${index + 1}</td>
                <td><strong>${item.Instrument}</strong></td>
                <td>$${item.Revenue.toFixed(2)}</td>
                <td>${item.LessonCount}</td>
                <td>${percentage}%</td>
                <td>${isInTop50 ? '<span class="badge bg-success">Yes</span>' : '<span class="badge bg-secondary">No</span>'}</td>
            </tr>
        `;
    }).join('');

    tbody.innerHTML = rows;
}

function populateStudentRevenueDistribution(totalRevenue, distributionData) {
    // Update summary stats
    document.getElementById('totalRevenueStudents').textContent = totalRevenue.toFixed(2);
    document.getElementById('studentsCount50').textContent = distributionData.studentsCount;
    document.getElementById('studentsRevenue50').textContent = distributionData.studentsRevenue.toFixed(2);

    // Populate table
    const tbody = document.getElementById('studentRevenueTable');
    if (!distributionData.students || distributionData.students.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" class="text-center">No data available</td></tr>';
        return;
    }

    const students50Set = new Set(distributionData.studentsFor50Percent);
    const rows = distributionData.students.map((item, index) => {
        const percentage = totalRevenue > 0 ? (item.Revenue / totalRevenue * 100).toFixed(1) : 0;
        const isInTop50 = students50Set.has(item.StudentName);
        const rowClass = isInTop50 ? 'table-warning' : '';
        
        return `
            <tr class="${rowClass}">
                <td>${index + 1}</td>
                <td><strong>${item.StudentName}</strong></td>
                <td>$${item.Revenue.toFixed(2)}</td>
                <td>${item.LessonCount}</td>
                <td>${percentage}%</td>
                <td>${isInTop50 ? '<span class="badge bg-warning">Yes</span>' : '<span class="badge bg-secondary">No</span>'}</td>
            </tr>
        `;
    }).join('');

    tbody.innerHTML = rows;
}