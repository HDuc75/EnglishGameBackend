using BCrypt.Net;
using EnglishGame.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EnglishGame.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        // 1) Seed Topics
        if (!await db.Topics.AnyAsync())
        {
            db.Topics.AddRange(
                new Topic { Name = "Travel", Description = "Asking directions, airport, hotel..." },
                new Topic { Name = "Daily Life", Description = "Shopping, food, routine..." },
                new Topic { Name = "Interview", Description = "Basic job interview Q&A..." }
            );
        }

        // 2) Seed Admin user
        if (!await db.Users.AnyAsync(u => u.Role == UserRole.Admin))
        {
            db.Users.Add(new User
            {
                FullName = "Admin",
                Email = "admin@demo.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@12345"),
                Role = UserRole.Admin,
                Level = CefrLevel.B1
            });
        }

        await db.SaveChangesAsync();

        // 3) Seed Quiz bank (AiContents) nếu chưa có
        if (await db.AiContents.AnyAsync(x => x.Type == AiContentType.Quiz && x.Status == ReviewStatus.Approved))
            return;

        var admin = await db.Users.AsNoTracking().FirstAsync(u => u.Role == UserRole.Admin);
        var topics = await db.Topics.AsNoTracking().Where(t => t.IsActive).ToListAsync();

        foreach (var topic in topics)
        {
            // A2
            await EnsureSeedQuiz(db, admin.Id, topic, CefrLevel.A2, BuildSeedQuestions(topic.Name, "A2"));
            // B1
            await EnsureSeedQuiz(db, admin.Id, topic, CefrLevel.B1, BuildSeedQuestions(topic.Name, "B1"));
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureSeedQuiz(
        AppDbContext db,
        Guid createdByUserId,
        Topic topic,
        CefrLevel level,
        List<object> questions)
    {
        var exists = await db.AiContents.AnyAsync(x =>
            x.Type == AiContentType.Quiz &&
            x.TopicId == topic.Id &&
            x.Level == level &&
            x.Status == ReviewStatus.Approved);

        if (exists) return;

        var levelStr = level == CefrLevel.A2 ? "A2" : "B1";

        var payload = new
        {
            type = "quiz",
            level = levelStr,
            topic = topic.Name,
            questions
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        var content = new AiContent
        {
            Type = AiContentType.Quiz,
            Level = level,
            TopicId = topic.Id,
            Status = ReviewStatus.Approved,
            RawPrompt = $"[SEED] topic={topic.Name} level={levelStr}",
            RawResponse = "[SEED]",
            ContentJson = json,
            CreatedByUserId = createdByUserId,
            ReviewedByUserId = createdByUserId,
            ReviewedAtUtc = DateTimeOffset.UtcNow
        };

        db.AiContents.Add(content);
    }

    private static List<object> BuildSeedQuestions(string topicName, string level)
    {
        // 5 câu / topic / level để luôn đủ MIN_Q=5
        // format: mcq + answer is string (đúng schema GameService)
        if (topicName.Equals("Travel", StringComparison.OrdinalIgnoreCase))
        {
            if (level == "A2")
            {
                return new List<object>
                {
                    new { no = 1, format = "mcq", question = "You are at the airport. Where do you check in?", options = new [] { "At the check-in desk", "At the swimming pool", "At the cinema", "At the library" }, answer = "At the check-in desk", explanation = "You check in at the check-in desk." },
                    new { no = 2, format = "mcq", question = "Which sentence asks for directions?", options = new [] { "Where is the bus stop?", "I like apples.", "She is a teacher.", "We are happy." }, answer = "Where is the bus stop?", explanation = "That sentence asks for directions." },
                    new { no = 3, format = "mcq", question = "At a hotel, you want a room key. You say:", options = new [] { "Can I have the key, please?", "Can I have a sandwich?", "Can I have a bike?", "Can I have a book?" }, answer = "Can I have the key, please?", explanation = "You ask for the key politely." },
                    new { no = 4, format = "mcq", question = "Choose the correct word: 'I need a ___ to go to another country.'", options = new [] { "passport", "pencil", "plate", "pillow" }, answer = "passport", explanation = "A passport is used for international travel." },
                    new { no = 5, format = "mcq", question = "You are lost. What do you say first?", options = new [] { "Excuse me, can you help me?", "Goodbye!", "I don’t like this.", "It is raining." }, answer = "Excuse me, can you help me?", explanation = "Start politely before asking for directions." }
                };
            }

            return new List<object>
            {
                new { no = 1, format = "mcq", question = "Choose the best reply: 'Could you tell me how to get to the station?'", options = new [] { "Sure. Go straight and turn left.", "I’m reading a book.", "I don’t like stations.", "It’s very delicious." }, answer = "Sure. Go straight and turn left.", explanation = "It gives directions politely." },
                new { no = 2, format = "mcq", question = "At immigration, the officer asks about your trip. You say:", options = new [] { "I’m here on vacation for a week.", "I’m here to buy a notebook.", "I’m here to wash my car.", "I’m here to sleep all day." }, answer = "I’m here on vacation for a week.", explanation = "That answers the purpose and duration of the trip." },
                new { no = 3, format = "mcq", question = "Choose the correct phrase: 'My flight was ___ due to bad weather.'", options = new [] { "delayed", "delicious", "borrowed", "painted" }, answer = "delayed", explanation = "Flights can be delayed." },
                new { no = 4, format = "mcq", question = "You missed your connection. What should you ask?", options = new [] { "Can you rebook me on the next flight?", "Can you sell me a chair?", "Can you fix my phone screen?", "Can you teach me math?" }, answer = "Can you rebook me on the next flight?", explanation = "Rebook is the right action for missed flights." },
                new { no = 5, format = "mcq", question = "Choose the most natural sentence:", options = new [] { "Could I have a window seat, please?", "Could I have a window, please to sit?", "Window seat I want, please?", "Give me window." }, answer = "Could I have a window seat, please?", explanation = "This is polite and natural English." }
            };
        }

        if (topicName.Equals("Daily Life", StringComparison.OrdinalIgnoreCase))
        {
            if (level == "A2")
            {
                return new List<object>
                {
                    new { no = 1, format = "mcq", question = "Choose the correct sentence for a routine:", options = new [] { "I get up at 7 a.m.", "I am a book.", "She eats the sky.", "We are two chairs." }, answer = "I get up at 7 a.m.", explanation = "It describes a daily routine." },
                    new { no = 2, format = "mcq", question = "At a shop, you ask the price. You say:", options = new [] { "How much is this?", "Where is the moon?", "What is your name?", "Do you swim?" }, answer = "How much is this?", explanation = "That asks for the price." },
                    new { no = 3, format = "mcq", question = "Choose the correct word: 'I buy bread at the ___.'", options = new [] { "bakery", "airport", "hospital", "museum" }, answer = "bakery", explanation = "A bakery sells bread." },
                    new { no = 4, format = "mcq", question = "You are hungry. What do you say?", options = new [] { "I’d like some food.", "I’d like some shoes.", "I’d like some rain.", "I’d like some pencils." }, answer = "I’d like some food.", explanation = "You ask for food when hungry." },
                    new { no = 5, format = "mcq", question = "Choose the correct sentence:", options = new [] { "She cooks dinner every day.", "She cook dinner every day.", "She cooking dinner every day.", "She cooked dinner every day now." }, answer = "She cooks dinner every day.", explanation = "Present simple for habits." }
                };
            }

            return new List<object>
            {
                new { no = 1, format = "mcq", question = "Choose the best sentence:", options = new [] { "I usually go grocery shopping on Sundays.", "I grocery go usually shopping Sundays.", "Usually I Sundays grocery shopping go.", "Go I shopping Sundays usually grocery." }, answer = "I usually go grocery shopping on Sundays.", explanation = "Correct word order in present simple." },
                new { no = 2, format = "mcq", question = "You bought the wrong size. What do you say?", options = new [] { "Could I exchange this for a different size?", "Could I eat this size?", "Could I exchange my phone number?", "Could I different this size?" }, answer = "Could I exchange this for a different size?", explanation = "Exchange is used for changing an item." },
                new { no = 3, format = "mcq", question = "Choose the correct word: 'I’m running out of ___, so I need to go shopping.'", options = new [] { "milk", "music", "mountain", "mirror" }, answer = "milk", explanation = "Milk is something you can run out of at home." },
                new { no = 4, format = "mcq", question = "Pick the best reply: 'Do you have any plans tonight?'", options = new [] { "Yes, I’m going to meet a friend.", "Yes, I have a blue.", "No, I am a plan.", "Tonight is table." }, answer = "Yes, I’m going to meet a friend.", explanation = "Natural reply about plans." },
                new { no = 5, format = "mcq", question = "Choose the most natural phrase:", options = new [] { "I’d like a table for two, please.", "I like table two please.", "Give me table two.", "I table two want." }, answer = "I’d like a table for two, please.", explanation = "Polite request at a restaurant." }
            };
        }

        // Interview
        if (level == "A2")
        {
            return new List<object>
            {
                new { no = 1, format = "mcq", question = "A common interview question is:", options = new [] { "Can you introduce yourself?", "Where is the ocean?", "Do you like pizza?", "What time is the bus?" }, answer = "Can you introduce yourself?", explanation = "Interviewers often ask you to introduce yourself." },
                new { no = 2, format = "mcq", question = "Choose the correct sentence:", options = new [] { "I have experience in sales.", "I has experience in sales.", "I having experience in sales.", "I experienced in sales have." }, answer = "I have experience in sales.", explanation = "Correct grammar." },
                new { no = 3, format = "mcq", question = "You talk about your strengths. You say:", options = new [] { "I am hardworking.", "I am sleeping.", "I am a chair.", "I am rain." }, answer = "I am hardworking.", explanation = "Hardworking is a strength." },
                new { no = 4, format = "mcq", question = "Choose the best reply: 'Why do you want this job?'", options = new [] { "Because I want to learn and grow.", "Because I am a job.", "Because it is raining.", "Because shoes are blue." }, answer = "Because I want to learn and grow.", explanation = "A clear, positive reason." },
                new { no = 5, format = "mcq", question = "At the end, you can say:", options = new [] { "Thank you for your time.", "Thank you for your shoes.", "Thank you for the sun.", "Thank you for the chair." }, answer = "Thank you for your time.", explanation = "Polite closing in an interview." }
            };
        }

        return new List<object>
        {
            new { no = 1, format = "mcq", question = "Choose the best reply: 'Tell me about yourself.'", options = new [] { "I’m a recent graduate and I enjoy working with people.", "I am about yourself.", "Myself is very table.", "Tell you about me no." }, answer = "I’m a recent graduate and I enjoy working with people.", explanation = "A concise, natural self-introduction." },
            new { no = 2, format = "mcq", question = "Pick the most natural sentence:", options = new [] { "I’m confident I can contribute to your team.", "I confident contribute can team your.", "Contribute I your team can confident.", "I can team contribute your confident." }, answer = "I’m confident I can contribute to your team.", explanation = "Correct, natural word order." },
            new { no = 3, format = "mcq", question = "Choose the best answer to: 'What are your strengths?'", options = new [] { "I’m organized and I learn quickly.", "My strengths are raining.", "Strength is my phone.", "I strong." }, answer = "I’m organized and I learn quickly.", explanation = "Clear strengths with good grammar." },
            new { no = 4, format = "mcq", question = "If you don’t know something, you can say:", options = new [] { "I’m not sure, but I can find out.", "I don’t know and I won’t.", "Not sure is my name.", "Find out? No." }, answer = "I’m not sure, but I can find out.", explanation = "Professional and positive response." },
            new { no = 5, format = "mcq", question = "Choose the best closing:", options = new [] { "Thank you for the opportunity. I look forward to hearing from you.", "Thank you opportunity hear you.", "Opportunity thank forward you.", "Hearing you thank." }, answer = "Thank you for the opportunity. I look forward to hearing from you.", explanation = "Polite and professional closing." }
        };
    }
}