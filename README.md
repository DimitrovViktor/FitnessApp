# <div align="center"> FitnessApp </div>

<div align="center">

[![C#](https://custom-icon-badges.demolab.com/badge/C%23-%23239120.svg?logo=cshrp&logoColor=white)](#)
[![.NET](https://img.shields.io/badge/.NET-512BD4?logo=dotnet&logoColor=fff)](#)
[![Blazor](https://img.shields.io/badge/Blazor-512BD4?logo=blazor&logoColor=fff)](#)
[![SQLite](https://img.shields.io/badge/SQLite-%2307405e.svg?logo=sqlite&logoColor=white)](#)
[![MIT License](https://img.shields.io/badge/License-MIT-green.svg)](https://choosealicense.com/licenses/mit/)

A full stack fitness application built with Blazor and SQLite.

</div>


---

![Application](https://github.com/user-attachments/assets/e5c94e15-70f6-4ff7-b6a1-004d345f42bb)

---

## Login and Onboarding

![Login Page](https://github.com/user-attachments/assets/5fec8c86-7326-421c-bcac-04bd0af30348)

Users register with an email and password, then go through onboarding. Onboarding collects the details the app needs for its calculations, such as body weight and height.

### Account types:
 - User - full access to training, diet, progress and social
 - Admin - everything above, plus the admin panel

---

## Global Features

### Themes

Four presets are included: Night, Midnight, Day and Lavender. The switcher sits in the nav bar and applies instantly across every page.

### Notifications

![Notifications](https://github.com/user-attachments/assets/9dd93b8e-5cbe-45d0-82a7-916c629932d9)

A bell in the nav bar opens the notifications modal with everything recent. New events also appear as popups in the top right corner, and those close on their own.

Users are notified when:
 - Someone sends them a direct message
 - Someone likes or reposts their post
 - They have a workout or meal scheduled in the next 24 hours

Social notifications fire once per event. Scheduled session reminders keep going until the session time passes, then stop. Both types can be turned off in settings.

### Units

Weight, distance and energy units are set once in settings. Every page follows them, including charts and logs.

---

## Dashboard

![Dashboard](https://github.com/user-attachments/assets/beaf925a-af0f-4166-aaed-dc243ab38482)

The landing page after login. It shows the day at a glance.

### Features:

 - Stats for completed workouts, streak, calories burned and calories eaten
 - Quick Actions for starting a workout, logging cardio, logging food, logging a measurement or scheduling a session
 - Today's plan with scheduled workouts and meals, startable from the card
 - A reminder banner when a session is due
 - Readiness notes with energy and mood ratings

---

## Schedule

![Schedule](https://github.com/user-attachments/assets/67bb788a-106f-46a3-bb5f-5482a2bd00da)

Planning view for workouts and meals.

### Features:

 - Schedule workouts and meals on any date and time
 - Calendar and list views
 - Start, postpone or delete a scheduled session
 - Scheduled items feed the dashboard and the 24 hour reminders

---

## Training Section

### Workouts Page

![Workouts](https://github.com/user-attachments/assets/362dc8b1-a73f-4bd7-9192-6d6a8de1fd7e)

 - Build custom workouts from the exercise library
 - Set sets, reps, weight and rest per exercise
 - Superset support with their own rest timers
 - Browse premade workouts and save them
 - Recent activity list

### Programs Library Page

![Programs](https://github.com/user-attachments/assets/8066bb41-a514-49b5-8cad-4eac4907fad1)

 - Multi week programs with descriptions
 - The workouts inside each program
 - Recommended schedules
 - Muscle volume breakdown per program


### Exercise Library Page

![Exercise Library](https://github.com/user-attachments/assets/0c68e17e-ed52-48dd-9e08-f9baa3f84aa4)

 - Exercises sorted by muscle group and equipment
 - Descriptions and instructions
 - Video links
 - Example weights

### Live Session

![Live Session](https://github.com/user-attachments/assets/100961b3-1d3c-4579-ac5c-dd5d68406df8)

 - Run a workout set by set and tick sets off as you go
 - Rest timer between sets, with an auto start option
 - Skip exercises or finish early
 - Cardio tracking for runs and walks
 - Calories are estimated from MET values and the user's current body weight

---

## Progress Page

![Progress](https://github.com/user-attachments/assets/89c41600-4349-436c-b0e1-5d988ab9d935)

Split into Body, Training and Diet.

### Body:
 - Log weight, body fat, chest, waist, arms, thighs and hips
 - Charts for any measurement over time
 - Full measurement history with inline editing
 - Logging a new weight updates the profile weight, so calorie estimates stay accurate

### Training:
 - Total workouts, streak, weekly volume and average session length
 - Weekly muscle coverage, so undertrained muscles are easy to spot
 - Weekly energy burn
 - Personal records with a progression chart
 - 30 day consistency grid

### Diet:
 - Average calories and macros
 - Calorie goal adherence
 - Logging streak

---

## Diet Section

### Diet Page

![Diet](https://github.com/user-attachments/assets/11c26296-cb56-4e16-a03c-128256ca9c91)

 - Search foods and log them by weight
 - Daily totals for calories, protein, carbs and fat
 - Scheduled meals for the day, logged in one tap
 - Edit or remove any entry

### Diet Plans Page

![Diet Plans](https://github.com/user-attachments/assets/f8e1b625-78b6-4c4e-a1d0-ee6639c3c232)

 - Build plans for cutting, maintenance or bulking
 - Add foods with target amounts
 - Browse premade plans

### Foods Page

![Foods](https://github.com/user-attachments/assets/d2d2ad82-bd66-45da-9918-bd778325ce87)

 - Foods sorted into categories
 - Calories and macros per 100g
 - Add custom foods

---

## Social Section

### Direct Messages

![Direct Messages](https://github.com/user-attachments/assets/517a5ae0-2a24-4669-8e34-4ce1597e4610)

 - Private messaging with live delivery
 - Image and file attachments
 - Share workouts and programs straight into a chat
 - Online status for other users

### Friends

![Friends](https://github.com/user-attachments/assets/ef8d274a-a3ac-43cc-8d15-8d1c2e86a72d)

 - Search users by username
 - Send, accept and decline friend requests
 - Friends list with status and a shortcut to message them

### Global

![Global Feed](https://github.com/user-attachments/assets/7a37d2ff-9aea-4ec9-af70-af4a4bed71c9)

 - Post to everyone
 - Attach a completed workout or a program to a post
 - Likes, reposts and comments

---

## Profile Page

![Profile](https://github.com/user-attachments/assets/7ab87572-94d7-45e6-8f2e-5037bf237d31)

 - Display name, username and bio
 - Body details used by the app's calculations
 - Privacy controls for what other users can see
 - Activity and stats

---

## Settings Page

![Settings](https://github.com/user-attachments/assets/83b88136-9c69-4fc4-9ef7-02a523fd9b7d)

 - Theme selection
 - Units for weight, distance and energy
 - Default rest timer and auto start
 - Week start day
 - Notification toggles for reminders and social events

---

## Admin Page

![Admin](https://github.com/user-attachments/assets/48d27e6c-4ec3-4df8-99f8-40b968d368bf)

Staff only. Full CRUD over the database.

### Managed tables:
 - Users
 - Exercises
 - Foods
 - Muscle Groups
 - Equipment
 - Programs
 - Workouts
 - Diet Plans

---

## Requirements

- **.NET 10 SDK**
- **Git**

---

## Setup

Clone the repository and restore packages:

```sh
git clone https://github.com/DimitrovViktor/FitnessApp
cd FitnessApp
dotnet restore
```

### Database

The database is created and seeded on first run. No migration step is needed.

The app creates `fitnessapp.db` in the project folder.

### Running

```sh
dotnet run
```

Then open the URL printed in the terminal, usually https://localhost:5001

### Admin account

A helper project in `tools/AddAdmin` promotes an account to admin. Run it from its own folder:

```sh
cd tools/AddAdmin
dotnet run
```

---

## Project Structure

```
FitnessApp/
├── Components/
│   ├── Data/         EF Core DbContext and seeding
│   ├── Layout/       Nav, modals, notifications, shared UI
│   ├── Models/       Entities
│   ├── Pages/        Every page in the app
│   └── Services/     Business logic
├── wwwroot/          Global CSS, themes, icons, uploads
├── tools/AddAdmin/   Admin account helper
└── Program.cs        Startup, DI and database setup
```

Page specific styles inside `.razor.css` files. Shared styles and the theme variables live in `wwwroot/app.css`.

---

## Future Work

UI Changes
