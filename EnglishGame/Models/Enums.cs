namespace EnglishGame.Models;

public enum UserRole { Learner = 0, Admin = 1 }
public enum CefrLevel { A2 = 0, B1 = 1 }
public enum AiContentType { Quiz = 0, Dialogue = 1, WritingPrompt = 2, FullExam = 3 }
public enum ReviewStatus { Pending = 0, Approved = 1, Rejected = 2 }
public enum GameMode { QuizSprint = 0, DialogueRoleplay = 1, Writing = 2, FullExam = 3 }

/// <summary>Skill category for each quiz question.</summary>
public enum SkillType { Vocabulary = 0, Reading = 1, Listening = 2, Grammar = 3, Speaking = 4, Writing = 5 }
