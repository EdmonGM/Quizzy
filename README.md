# Quizzy

A modern quiz platform built with ASP.NET Core 10 and Next.js, enabling teachers to create and manage quizzes while students can take them and track their progress.

![Tech Stack](https://img.shields.io/badge/Backend-ASP.NET%20Core%2010-blue?logo=dotnet)
![Frontend](https://img.shields.io/badge/Frontend-Next.js-black?logo=next.js)
![Database](https://img.shields.io/badge/Database-PostgreSQL-blue?logo=postgresql)

## Features

### For Teachers

- 📝 Create and manage multiple-choice quizzes
- 📂 Organize quizzes by categories
- 🔒 **Invite-only quizzes** with access codes
- 📊 View student performance and analytics
- ⏱️ Set time limits and passing scores
- 🔄 Configure maximum attempts
- ✏️ Full CRUD operations for questions and answers

### For Students

- 🎓 Browse and search published quizzes
- 🔑 Access invite-only quizzes with codes
- 📝 Take quizzes with real-time answer saving
- 📈 View attempt history and scores
- 🏆 Track personal statistics and progress
- ⏰ Time-limited quiz support

### For Admins

- 👥 User management
- 🏷️ Category management
- 📊 Platform-wide statistics
- 🔐 Role assignment (Teacher/Student)

## Tech Stack

### Backend

| Technology                | Purpose                        |
| ------------------------- | ------------------------------ |
| **ASP.NET Core 10**       | Web API framework              |
| **Entity Framework Core** | ORM for database access        |
| **PostgreSQL**            | Primary database               |
| **ASP.NET Core Identity** | Authentication & authorization |
| **JWT**                   | Token-based authentication     |
| **AutoMapper**            | Object-object mapping          |
| **FluentValidation**      | Request validation             |
| **Serilog**               | Structured logging             |

### Frontend

| Technology          | Purpose                         |
| ------------------- | ------------------------------- |
| **Next.js 15**      | React framework with App Router |
| **TypeScript**      | Type-safe JavaScript            |
| **shadcn/ui**       | UI component library            |
| **TanStack Query**  | Server state management         |
| **TanStack Table**  | Advanced table handling         |
| **React Hook Form** | Form management                 |
| **Zod**             | Schema validation               |
| **Tailwind CSS**    | Utility-first CSS               |

## Project Structure

```
Quizzy/
├── src/
│   ├── Quizzy.Api/          # ASP.NET Core Web API
│   │   ├── Controllers/     # API endpoints
│   │   ├── Services/        # Business logic
│   │   ├── Data/            # EF Core DbContext
│   │   ├── Models/          # Database entities
│   │   ├── DTOs/            # Data transfer objects
│   │   └── Middleware/      # Custom middleware
│   │
│   └── Quizzy.Web/          # Next.js Frontend
│       ├── app/             # App Router pages
│       ├── components/      # React components
│       │   ├── ui/          # shadcn components
│       │   ├── quizzes/     # Quiz-specific components
│       │   └── shared/      # Shared components
│       ├── hooks/           # Custom React hooks
│       ├── lib/             # Utilities & API clients
│       └── types/           # TypeScript types
│
├── docs/
│   ├── db_schema.md         # Database schema documentation
│   └── api_endpoints.md     # API endpoint documentation
│
└── tests/
    ├── Quizzy.Api.Tests/    # API unit tests
    └── Quizzy.Web.Tests/    # Frontend tests
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js 20+
- PostgreSQL 15+
- Git

### Backend Setup

1. **Clone the repository**

   ```bash
   git clone https://github.com/your-org/quizzy.git
   cd Quizzy/src/Quizzy.Api
   ```

2. **Configure database connection**

   Update `appsettings.Development.json`:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=Quizzy;Username=postgres;Password=yourpassword"
     }
   }
   ```

3. **Run migrations**

   ```bash
   dotnet ef database update
   ```

4. **Seed initial data (optional)**

   ```bash
   dotnet run --seed
   ```

5. **Start the API**

   ```bash
   dotnet run
   ```

   API will be available at `https://localhost:5001` and `http://localhost:5000`

### Frontend Setup

1. **Navigate to frontend directory**

   ```bash
   cd ../Quizzy.Web
   ```

2. **Install dependencies**

   ```bash
   npm install
   ```

3. **Configure environment**

   Create `.env.local`:

   ```env
   NEXT_PUBLIC_API_URL=http://localhost:5000
   NEXT_PUBLIC_APP_NAME=Quizzy
   ```

4. **Start development server**

   ```bash
   npm run dev
   ```

   Frontend will be available at `http://localhost:3000`

## Deployment

### Backend (ASP.NET Core)

1. Publish the application:

   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. Configure production settings in `appsettings.Production.json`

3. Deploy to your preferred hosting (Azure, AWS, IIS, etc.)

### Frontend (Next.js)

1. Build for production:

   ```bash
   npm run build
   ```

2. Start production server:

   ```bash
   npm start
   ```

3. Deploy to Vercel, Netlify, or your preferred platform

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For issues and questions:

- 📖 Check the [documentation](docs/)
- 🐛 Open an issue on GitHub
- 📧 Contact: support@quizzy.example.com

---
