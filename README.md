# 🛡️ DeadlineGuard — Smart Deadline & Penalty Tracker

[![.NET](https://img.shields.io/badge/.NET-10.0-512bd4.svg)](https://dotnet.microsoft.com/)
[![Database](https://img.shields.io/badge/Database-MySQL-4479A1.svg)](https://www.mysql.com/)
[![Architecture](https://img.shields.io/badge/Architecture-ASP.NET%20Core%20MVC-blue.svg)](https://learn.microsoft.com/aspnet/core)
[![Tests](https://img.shields.io/badge/Tests-22%20Passed-brightgreen.svg)](#-automated-testing)

**DeadlineGuard** is an ASP.NET Core MVC web application designed to help users manage important deadlines, track potential penalties, organize supporting documents, and receive reminders before due dates.

It includes an explainable priority-ranking system, deadline conflict detection, recurring deadlines, in-app notifications, and background reminder processing.

## 🌟 Key Features

- **Smart Priority Ranking:** Scores active deadlines and categorizes them by urgency, consequence, effort, and importance.
- **Conflict Detection:** Identifies deadline clashes and workload overload.
- **Dashboard & Analytics:** View upcoming deadlines, overdue tasks, and deadline statistics.
- **Recurring Deadlines:** Supports recurring schedules and preserves completed deadline history.
- **Reminder Background Service:** Processes scheduled reminders and supports in-app notifications and SMTP email configuration.
- **Document Attachments:** Supports deadline-related files with ownership checks for authorized downloads.
- **Audit History:** Tracks important deadline changes and completion activity.
- **Authentication:** User registration, login, and profile preferences using ASP.NET Core Identity.

## 🧠 Priority Engine

DeadlineGuard uses a weighted scoring model to help users understand which deadlines need attention first.

The conceptual weighting is:

- Urgency: 40%
- Consequence: 30%
- Effort: 15%
- Importance: 15%

The application presents priority levels and supporting explanations to help users plan their next actions.

## 🏗️ Technology Stack

- **Framework:** .NET 10 / ASP.NET Core MVC
- **Language:** C#
- **Database:** MySQL
- **ORM:** Entity Framework Core
- **Authentication:** ASP.NET Core Identity
- **Frontend:** Razor Views, Bootstrap 5, Bootstrap Icons
- **Charts:** Chart.js
- **Calendar:** FullCalendar
- **Background Processing:** `BackgroundService`
- **Testing:** .NET test project

## 🗄️ Database Design

The application uses Entity Framework Core and MySQL to manage application data.

Core entities include:

- Application users
- Deadlines
- Deadline attachments
- Reminders
- Notifications
- Deadline history
- User preferences

## 🚀 Getting Started

### Prerequisites

Install the following:

1. .NET 10 SDK
2. MySQL Server
3. Git

### 1. Clone the repository

```bash
git clone https://github.com/tanishapatel3008/Deadline-Guard.git
cd Deadline-Guard
```

### 2. Configure the database

Create a MySQL database named `deadline_guard`.

Configure the connection string locally using .NET User Secrets or an environment variable. Do not commit database passwords or other private credentials to GitHub.

Example connection-string format:

```text
Server=localhost;Port=3306;Database=deadline_guard;Uid=YOUR_MYSQL_USER;Pwd=YOUR_MYSQL_PASSWORD;
```

Replace the example username and password with your own local MySQL credentials.

### 3. Configure the connection string with User Secrets

From the repository root, run:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=deadline_guard;Uid=YOUR_MYSQL_USER;Pwd=YOUR_MYSQL_PASSWORD;" --project DeadlineGuard
```

Replace the placeholder values before running the command. User Secrets are for local development and are not a production secrets-management solution.

### 4. Restore dependencies

```bash
dotnet restore DeadlineGuard.slnx
```

### 5. Run the application

```bash
dotnet run --project DeadlineGuard
```

Open the local URL shown in the terminal.

The application runs database initialization during startup. Ensure MySQL is running and the connection string is configured correctly.

## 👤 Demo Data

The application can seed a demonstration user and sample deadlines when the environment variable `DEADLINEGUARD_DEMO_PASSWORD` is configured.

For local development, set a strong password in your environment before starting the application. Do not publish that password or commit it to source control.

If the variable is not configured, demo-user seeding is skipped. You can use the application's registration flow to create an account.

## ✉️ SMTP Email Configuration

SMTP credentials should be configured locally using .NET User Secrets or environment variables rather than being committed to the repository.

Example User Secrets commands:

```bash
dotnet user-secrets set "SmtpSettings:Server" "smtp.gmail.com" --project DeadlineGuard
dotnet user-secrets set "SmtpSettings:Port" "587" --project DeadlineGuard
dotnet user-secrets set "SmtpSettings:Username" "YOUR_EMAIL" --project DeadlineGuard
dotnet user-secrets set "SmtpSettings:Password" "YOUR_APP_PASSWORD" --project DeadlineGuard
dotnet user-secrets set "SmtpSettings:IsConfigured" "true" --project DeadlineGuard
```

Replace the placeholders with your own SMTP details. Email delivery depends on valid SMTP configuration and the application's notification settings.

## 🧪 Automated Testing

The solution includes automated tests for core application behavior.

Run the tests from the repository root:

```bash
dotnet test DeadlineGuard.slnx
```

**Latest verified result:** 22 tests passed, with no failures.

## 🔐 Security Notes

- Do not commit real passwords, API keys, SMTP credentials, or private connection strings.
- Keep local configuration and secrets outside source control.
- Use strong passwords for demo accounts and database users.
- Review configuration files before making the repository public.
- Configure production secrets through an appropriate hosting-platform secrets manager.

## 📌 Future Improvements

Potential improvements include distributed background-job scheduling for multi-instance deployments, additional notification channels, and production deployment automation.

---

**DeadlineGuard** — Organize your deadlines. Understand your priorities. Stay ahead.
