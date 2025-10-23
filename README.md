# Freelance Music Portal

A full-stack music lesson management application with a .NET backend API and static HTML/CSS/JavaScript frontend served separately.

## Quick Start

### Windows (PowerShell)
```powershell
.\start.ps1
```

### Linux/Mac (Bash)
```bash
./start.sh
```

**Note:** Make sure the script is executable. If you get permission errors, run:
```bash
chmod +x start.sh
```

Both scripts will:
- Start the ASP.NET Core backend API server on port 5271
- Start a Python static HTTP server for the frontend on port 8080
- Handle errors gracefully and provide troubleshooting tips

**Access the application:**
- **Frontend:** http://localhost:8080
- **Backend API:** http://localhost:5271/api

**Script Options:**
- **Windows:** `.\start.ps1 -UseHttps` or `.\start.ps1 -Help`
- **Linux/Mac:** `./start.sh --https` or `./start.sh -h`

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) for the backend API
- [Python 3](https://www.python.org/downloads/) for the static frontend server
- **Windows:** PowerShell (comes pre-installed)
- **Linux/Mac:** Bash shell (comes pre-installed on most systems)

## Architecture

This application uses a **separated frontend/backend architecture**:

- **Backend:** Pure REST API built with ASP.NET Core (.NET 9)
  - Serves only JSON API responses
  - No MVC views or static file serving
  - Runs on port 5271 (HTTP) or 7107 (HTTPS)
  - All endpoints are prefixed with `/api/`

- **Frontend:** Static HTML/CSS/JavaScript
  - Served by Python's built-in HTTP server
  - Runs on port 8080
  - Makes AJAX calls to the backend API
  - No server-side rendering

## Project Structure

```
pa-2/
├── backend/           # .NET Web API
│   ├── Controllers/   # API endpoints (HomeController, StudentController, etc.)
│   ├── Models/        # Data models (User, Lesson, Availability, etc.)
│   ├── Services/      # Business logic (DatabaseService, AuthenticationService)
│   └── Program.cs     # API configuration (CORS, routing, etc.)
├── frontend/          # Static HTML/CSS/JS
│   ├── resources/
│   │   ├── scripts/   # JavaScript files (main.js, student.js, etc.)
│   │   ├── styles/    # CSS files
│   │   └── images/    # Static assets
│   ├── index.html     # Landing page
│   ├── student.html   # Student dashboard
│   ├── teacher.html   # Teacher dashboard
│   └── admin.html     # Admin dashboard
├── start.ps1         # Windows PowerShell startup script
└── start.sh          # Linux/Mac bash startup script
```

## Admin Access

The application includes a comprehensive admin dashboard for system management and analytics.

**Default Admin Account:**
- **Email:** admin@freelancemusic.com
- **Password:** admin123

**Note:** Additional admin accounts can be created through the registration form by selecting the "Admin" role.

**Access the Admin Dashboard:**
1. Start the application using the startup scripts
2. Navigate to: http://localhost:8080/admin.html
3. Login with the admin credentials above

**Admin Features:**
- **Dashboard Overview:** View total lessons, teachers, students, and key metrics
- **Lesson Management:** View all lessons with filtering and sorting capabilities
- **Revenue Analytics:** Quarterly revenue reports and revenue distribution analysis
- **User Metrics:** Track user registration and activity patterns
- **Calendar View:** Visual calendar showing all scheduled lessons
- **Reports:** Detailed analytics including popular instruments and referral sources

## API Endpoints

All endpoints are prefixed with `/api/`. Examples:

- `POST /api/home/login` - User authentication
- `POST /api/home/register` - User registration
- `GET /api/student/availabilities` - Get teacher availabilities
- `POST /api/student/book` - Book a lesson
- `GET /api/teacher/lessons/{teacherId}` - Get teacher's lessons
- `POST /api/teacher/availability` - Add teacher availability
- `GET /api/admin/dashboard` - Get admin dashboard data
- `GET /api/admin/lessons` - Get all lessons with filtering/sorting
- `GET /api/admin/reports` - Get comprehensive analytics reports

## Development

### Running the Backend Only
```bash
cd backend
dotnet run --launch-profile http
```

### Running the Frontend Only
```bash
cd frontend
python -m http.server 8080
# or python3 -m http.server 8080
```

### Making Changes

**Backend Changes:**
- Modify controllers in `backend/Controllers/`
- Update models in `backend/Models/`
- Backend will auto-reload on save with `dotnet watch run`

**Frontend Changes:**
- Edit HTML files in `frontend/`
- Edit JavaScript in `frontend/resources/scripts/`
- Edit CSS in `frontend/resources/styles/`
- Just refresh the browser - no build step required

## Troubleshooting

- **Port conflicts:** 
  - Backend uses ports 5271 (HTTP) or 7107 (HTTPS)
  - Frontend uses port 8080
  - Close other applications using these ports

- **Missing .NET SDK:** Ensure .NET 9 SDK is installed and accessible (run `dotnet --version`)

- **Missing Python:** Ensure Python 3 is installed (run `python --version` or `python3 --version`)

- **Permission errors (Linux/Mac):** Make sure start.sh is executable: `chmod +x start.sh`

- **Wrong directory:** Always run the scripts from the project root directory (where the scripts are located)

- **CORS errors:** The backend is configured to allow all origins. If you still get CORS errors, check that the backend is running.

- **API connection errors:** Make sure both servers are running. The frontend expects the backend at `http://localhost:5271/api`

- **Port checking:**
  - Windows: `netstat -an | findstr :5271` or `findstr :8080`
  - Linux/Mac: `lsof -i :5271` or `lsof -i :8080`

- **Dependencies:** Run `dotnet restore` in the backend directory if you get missing package errors

- **Script errors:** Both scripts provide detailed error messages and troubleshooting tips when they fail
