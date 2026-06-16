public class StudentRecord
{
    public string Id { get; set; }
    public string StudentId { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
    public decimal GPA { get; set; }
    public DateTime EnrolledAt { get; set; }

    public StudentRecord(string id, string studentId, string name, int age, decimal gpa, DateTime enrolledAt)
    {
        Id = id;
        StudentId = studentId;
        Name = name;
        Age = age;
        GPA = gpa;
        EnrolledAt = enrolledAt;
    }
}