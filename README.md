# SmartPlace

## AI-Powered College Placement Management System

SmartPlace is a full-stack college placement management system built using **ASP.NET Core Web API, ASP.NET Core MVC, Entity Framework Core, SQL Server, ASP.NET Core Identity, JWT authentication, and AI-assisted job matching**.

The platform connects **Students, Recruiters, Placement Officers, and Administrators** through a centralized placement workflow.

Unlike a traditional placement portal that only displays job openings, SmartPlace evaluates a student's **academic eligibility** and combines it with **resume-based skill analysis** to generate intelligent job recommendations.

---

## Project Overview

College placement processes often involve multiple disconnected activities such as:

- Student profile management
- Academic eligibility verification
- Company registration
- Job posting
- Resume processing
- Application management
- Interview scheduling
- Candidate selection
- Placement tracking

SmartPlace integrates these activities into a single role-based platform.

The system first determines whether a student satisfies a company's academic requirements and then uses extracted resume skills to rank eligible opportunities.

This creates a two-stage recommendation process:

```text
Academic Eligibility
        ↓
Eligible Jobs
        ↓
Resume Skill Analysis
        ↓
Skill Matching
        ↓
AI-Assisted Job Recommendations
```

---

# Key Features

## Student Portal

Students can:

- Register and securely log in
- Create and update their academic profile
- Enter 10th standard percentage
- Enter 12th standard percentage
- Enter CGPA
- Enter current backlogs
- Select department/branch
- Enter graduation year
- Upload a PDF resume
- Extract skills from the resume
- Browse published jobs
- View all jobs
- View academically eligible jobs
- View academically ineligible jobs
- Understand why they are not eligible for a job
- Receive AI-assisted job recommendations
- View matching and missing skills
- Apply for eligible jobs
- Track application status
- View interview schedules
- View interview results
- View final placement information

---

## Recruiter Portal

Recruiters can:

- Register and log in
- Register their company
- View company approval status
- Create jobs after company approval
- Define academic eligibility criteria
- View applications
- Shortlist candidates
- Schedule interview rounds
- Update interview results
- Track selected students

A recruiter cannot freely publish jobs for an unverified company. Company registration is tied to an approval workflow managed by authorized placement personnel.

---

## Placement Officer Portal

Placement Officers can:

- Monitor placement activities
- Review registered companies
- Approve or reject companies
- View students
- View jobs
- Monitor applications
- Monitor interview rounds
- Manage placement records
- Track the overall recruitment process

---

## Administrator Portal

Administrators have system-level management capabilities including:

- User management
- Role-aware account administration
- Student account management
- Recruiter account management
- Company oversight
- Placement system administration

---

# Academic Eligibility Engine

One of the core features of SmartPlace is automatic job eligibility evaluation.

Recruiters can specify requirements such as:

- Required department/branch
- Minimum 10th percentage
- Minimum 12th percentage
- Minimum CGPA
- Maximum allowed backlogs
- Required graduation year

For example:

```text
Required Department: Computer Science
Minimum 10th Percentage: 70%
Minimum 12th Percentage: 70%
Minimum CGPA: 7.0
Maximum Backlogs: 0
Graduation Year: 2027
```

SmartPlace compares these requirements against the student's academic profile.

Conceptually:

```text
Department Match
        AND
10th Percentage >= Required Percentage
        AND
12th Percentage >= Required Percentage
        AND
CGPA >= Required CGPA
        AND
Backlogs <= Maximum Allowed Backlogs
        AND
Graduation Year == Required Graduation Year
```

Only students satisfying the academic requirements are considered academically eligible.

This validation is not intended to exist only as a frontend indicator. Eligibility is enforced as part of the application's placement workflow.

---

# AI-Assisted Job Recommendation System

SmartPlace goes beyond basic academic eligibility.

After identifying eligible jobs, the system uses the student's extracted resume skills to evaluate how closely the student matches different opportunities.

## Recommendation Flow

```text
Student Resume
      ↓
Resume Processing
      ↓
Skill Extraction
      ↓
Student Skill Profile
      ↓
Academic Eligibility Filtering
      ↓
Eligible Jobs
      ↓
Skill Matching
      ↓
Match Percentage
      ↓
Ranked Job Recommendations
```

The recommendation interface can present:

- Match percentage
- Matching skills
- Missing skills
- Academic eligibility
- Recommendation information

This separation is important:

> **Academic eligibility determines whether the student can apply. Skill matching determines how suitable an eligible job may be for the student.**

This prevents AI recommendations from bypassing company eligibility requirements.

---

# Resume & Skill Extraction

Students can upload their resume through the Student Portal.

SmartPlace processes the resume and extracts relevant technical skills which are associated with the student's profile.

Extracted skills are subsequently used by the job matching system.

Examples may include:

```text
C#
Python
Java
SQL
ASP.NET Core
Cybersecurity
Cloud Computing
Machine Learning
```

The detected skill set is visible from the student's dashboard and is used during job recommendation.

---

# Company Approval Workflow

SmartPlace implements a company verification workflow.

```text
Recruiter Registration
        ↓
Company Registration
        ↓
Pending Approval
        ↓
Placement Officer / Admin Review
        ↓
Approved / Rejected
        ↓
Approved Recruiter Can Continue Job Workflow
```

This prevents newly registered recruiter companies from immediately participating in the placement process without authorization.

---

# Recruitment Workflow

A typical SmartPlace recruitment process is:

```text
Recruiter Registers
        ↓
Company Registration
        ↓
Company Approval
        ↓
Job Creation
        ↓
Job Publication
        ↓
Student Eligibility Evaluation
        ↓
Student Applies
        ↓
Application Review
        ↓
Shortlisting
        ↓
Interview Round
        ↓
Interview Result
        ↓
Student Selection
        ↓
Placement Record
```

---

# Role-Based Access Control

SmartPlace uses four primary roles:

| Role | Main Responsibilities |
|---|---|
| Student | Profile, resume, jobs, applications, interviews and placement |
| Recruiter | Company, jobs, applications, interviews and candidate selection |
| PlacementOfficer | Company approval and placement process management |
| Admin | System and user administration |

API endpoints use authorization policies/role restrictions so users cannot access functionality belonging to unauthorized roles.

---

# Security Features

SmartPlace incorporates multiple security controls.

### Authentication

The backend uses **ASP.NET Core Identity** for account management and **JWT Bearer Authentication** for API authentication.

### Password Policy

Passwords are configured to require stronger credentials, including:

- Uppercase character
- Lowercase character
- Number
- Special/non-alphanumeric character
- Minimum password length

Example:

```text
SmartPlace@123
```

### Authorization

Controllers and endpoints use role-based authorization such as:

```text
Student
Recruiter
PlacementOfficer
Admin
```

### Resource Ownership

Sensitive student and recruiter operations are designed to verify that authenticated users access resources associated with their own account where applicable.

### Additional Security Controls

The application also uses concepts such as:

- Unique user email addresses
- JWT signature validation
- JWT issuer validation
- JWT audience validation
- JWT expiration validation
- Role-based endpoint restrictions
- Account lockout configuration
- Company approval enforcement
- Academic eligibility enforcement
- Duplicate application prevention
- Input validation
- Anti-forgery validation on MVC form operations

---

# Technology Stack

## Backend

- ASP.NET Core Web API
- C#
- Entity Framework Core
- ASP.NET Core Identity
- JWT Bearer Authentication
- REST API architecture

## Frontend

- ASP.NET Core MVC
- Razor Views
- HTML
- CSS
- Bootstrap
- JavaScript

## Database

- Microsoft SQL Server
- Entity Framework Core Migrations

## AI / Intelligent Features

- Resume skill extraction
- Job-skill matching
- Academic eligibility filtering
- AI-assisted job recommendations
- Match scoring

## Development Tools

- Visual Studio
- Swagger / OpenAPI
- Git
- GitHub
- SQL Server tooling

---

# System Architecture

SmartPlace follows a separated frontend/backend architecture.

```text
┌───────────────────────────────┐
│       SmartPlace.Web          │
│                               │
│ ASP.NET Core MVC              │
│ Razor Views                   │
│ Bootstrap / CSS               │
└───────────────┬───────────────┘
                │
                │ HTTPS / REST API
                │ JWT
                ▼
┌───────────────────────────────┐
│       SmartPlace.API          │
│                               │
│ Controllers                   │
│ Authentication               │
│ Authorization                │
│ Business Logic               │
│ AI Services                  │
│ Eligibility Services         │
└───────────────┬───────────────┘
                │
                │ Entity Framework Core
                ▼
┌───────────────────────────────┐
│          SQL Server           │
│                               │
│ Students                      │
│ Companies                     │
│ Jobs                          │
│ Applications                  │
│ Interviews                    │
│ Placements                    │
│ Skills                        │
│ Identity Tables               │
└───────────────────────────────┘
```

---

# Major Domain Entities

SmartPlace contains entities including:

### Student

Stores student academic and placement-related information.

Important information includes:

- Full name
- Email
- 10th percentage
- 12th percentage
- CGPA
- Backlogs
- Graduation year
- Department
- Skills

### Company

Stores recruiter/company information including:

- Company name
- Industry
- Location
- Website
- Description
- Approval status
- Recruiter ownership

### Job

Contains:

- Job title
- Description
- Package
- Location
- Employment type
- Minimum 10th percentage
- Minimum 12th percentage
- Minimum CGPA
- Maximum backlogs
- Required department
- Graduation year
- Application deadline
- Publication status

### Application

Connects a student to a job and maintains the recruitment status.

Typical statuses include:

```text
Applied
Shortlisted
Interview
Selected
Rejected
```

### InterviewRound

Stores information such as:

- Round name
- Scheduled date
- Mode
- Location / meeting link
- Status
- Result
- Remarks

### Placement

Stores final placement information including:

- Student
- Company
- Offered package
- Joining date
- Placement status
- Offer letter information

---

# Project Structure

A simplified structure of the solution is:

```text
SmartPlace
│
├── SmartPlace.API
│   │
│   ├── Controllers
│   ├── Data
│   ├── Models
│   ├── Services
│   ├── Migrations
│   ├── Program.cs
│   └── appsettings.json
│
├── SmartPlace.Web
│   │
│   ├── Controllers
│   ├── Services
│   ├── Views
│   │   ├── Account
│   │   ├── StudentDashboard
│   │   ├── RecruiterDashboard
│   │   ├── PlacementDashboard
│   │   ├── AdminDashboard
│   │   └── Shared
│   │
│   ├── wwwroot
│   │   └── css
│   │       └── site.css
│   │
│   └── Program.cs
│
└── SmartPlace.sln
```

---

# API Modules

The backend exposes REST APIs for major modules including:

```text
Authentication
Students
Departments
Skills
Companies
Jobs
Applications
Interview Rounds
Placements
Resume Processing
AI / Job Matching
Administration
```

Swagger/OpenAPI is available during development for API exploration and testing.

---

# Application Status Flow

Applications generally move through the recruitment pipeline as follows:

```text
Applied
   ↓
Shortlisted
   ↓
Interview
   ↓
Selected
```

A candidate may also move to:

```text
Rejected
```

depending on the recruitment decision.

---

# Interview Management

Recruiters can schedule interview rounds for candidates progressing through the recruitment process.

An interview can contain:

- Round name
- Date and time
- Online/offline mode
- Location or meeting URL
- Result
- Recruiter remarks

Students can view their interview information through their dashboard.

---

# Student Dashboard

The Student Dashboard provides a centralized view of the student's placement journey.

It displays academic information such as:

- 10th percentage
- 12th percentage
- CGPA
- Backlogs
- Graduation year
- Department

It also provides direct access to:

- Jobs & Eligibility
- AI Job Recommendations
- Resume & AI Skills
- Applications
- Interviews
- Placement Status

The dashboard also indicates whether the student's academic profile is complete before eligibility calculations are performed.

---

# Database Migrations

SmartPlace uses **Entity Framework Core Migrations** to manage database schema changes.

Typical Package Manager Console commands are:

```powershell
Add-Migration MigrationName
Update-Database
```

For example:

```powershell
Add-Migration EnhancedPlacementEligibility
Update-Database
```

---

# Getting Started

## Prerequisites

Install:

- Visual Studio with ASP.NET development workload
- .NET SDK compatible with the project
- Microsoft SQL Server
- SQL Server Management Studio or equivalent SQL Server tooling
- Git

---

## 1. Clone the Repository

```bash
git clone <repository-url>
```

Move into the project directory:

```bash
cd SmartPlace
```

---

## 2. Configure the Database

Configure the SQL Server connection string in the API configuration.

Example structure:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_CONNECTION_STRING"
  }
}
```

Do not commit production credentials or secrets to a public repository.

---

## 3. Configure JWT

Configure the required JWT settings using secure configuration.

The application expects settings for:

```text
Jwt:Key
Jwt:Issuer
Jwt:Audience
```

Sensitive keys should be stored using an appropriate secret-management mechanism rather than committed directly to source control.

---

## 4. Apply Database Migrations

Using Visual Studio Package Manager Console:

```powershell
Update-Database
```

Make sure **SmartPlace.API** is being used as the appropriate startup/migration project when applying migrations.

---

## 5. Run the Backend

Start:

```text
SmartPlace.API
```

During development, Swagger can be used to verify the API.

---

## 6. Run the Frontend

Start:

```text
SmartPlace.Web
```

The MVC application communicates with SmartPlace.API through the configured API base URL.

---

# Suggested Demo Flow

For demonstrating SmartPlace, the following sequence highlights the major features:

### 1. Student

```text
Login
→ Academic Profile
→ Resume Upload
→ Skill Extraction
→ Jobs
→ Eligible / Not Eligible Jobs
→ AI Recommendations
→ Apply
```

### 2. Recruiter

```text
Login
→ Company
→ Create Job
→ View Applications
→ Shortlist Student
→ Schedule Interview
→ Record Result
→ Select Candidate
```

### 3. Placement Officer

```text
Login
→ Review Companies
→ Approve Company
→ Monitor Recruitment
→ Manage Placement
```

### 4. Administrator

```text
Login
→ User Management
→ System Administration
```

---

# What Makes SmartPlace Different?

Traditional placement management systems generally focus on storing students, companies, jobs and applications.

SmartPlace adds an intelligent decision layer.

### Traditional approach

```text
Job Posted
    ↓
Student Browses
    ↓
Student Applies
```

### SmartPlace approach

```text
Job Posted
      ↓
Academic Eligibility Evaluation
      ↓
Eligible Opportunities
      ↓
Resume Skill Analysis
      ↓
Skill Matching
      ↓
AI-Assisted Ranking
      ↓
Student Applies
      ↓
Recruitment Workflow
```

This combines **placement management, eligibility automation and intelligent job matching** in one system.

---

# Future Enhancements

Possible future improvements include:

- Email notifications
- Interview reminders
- Placement analytics
- Recruiter analytics
- Student performance dashboards
- Advanced semantic resume analysis
- Skill-gap learning recommendations
- Job description embeddings
- Automated resume scoring
- Real-time notifications
- Cloud deployment
- Object storage for resumes and offer letters
- Multi-factor authentication
- Audit logging
- Exportable placement reports
- Mobile application support

---

# Testing

SmartPlace can be tested at multiple levels:

### API Testing

Swagger/OpenAPI can be used to verify:

- Authentication
- Authorization
- CRUD operations
- Eligibility
- Applications
- Interviews
- Placements
- Role restrictions

### Workflow Testing

Important end-to-end scenarios include:

```text
Student Registration
→ Profile
→ Resume
→ Skills
→ Job Eligibility
→ Application
→ Interview
→ Selection
→ Placement
```

and:

```text
Recruiter Registration
→ Company Registration
→ Company Approval
→ Job Creation
→ Candidate Management
```

---

# Security Considerations

SmartPlace follows several important security principles:

- Authentication is handled through ASP.NET Core Identity.
- API authorization is protected through JWT bearer tokens.
- Role-based access restricts sensitive endpoints.
- Resource ownership checks help prevent cross-user access.
- Company approval prevents unauthorized recruiter workflows.
- Academic eligibility is validated before application processing.
- Duplicate applications are prevented.
- MVC state-changing forms use anti-forgery protection.
- Password requirements enforce stronger credentials.
- Sensitive configuration should not be committed to source control.

---

# Conclusion

SmartPlace demonstrates how a conventional college placement management system can be enhanced with **automated eligibility evaluation and AI-assisted job matching**.

The project combines:

**Full-stack web development + REST APIs + relational database design + authentication + authorization + placement workflow automation + AI-assisted recommendations**

into a single integrated application.

The result is a platform where students can discover opportunities suited to both their academic qualifications and skills while recruiters and placement personnel can manage the recruitment lifecycle through dedicated role-based portals.

---

## Project

**SmartPlace — AI-Powered College Placement Management System**

Developed as an academic/capstone project.