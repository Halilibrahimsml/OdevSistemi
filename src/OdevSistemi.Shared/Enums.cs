namespace OdevSistemi.Shared;

/// <summary>Kullanıcı rolü. Eğitmen ve öğrenci farklı yetkilere sahiptir.</summary>
public enum UserRole
{
    Instructor = 0,
    Student = 1
}

/// <summary>Soru tipi: çoktan seçmeli veya klasik (açık uçlu).</summary>
public enum QuestionType
{
    MultipleChoice = 0,
    Classic = 1
}