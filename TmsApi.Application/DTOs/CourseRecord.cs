public class CourseRecord{
    public string Id{get;set;}
    public string Title{get;set;}
    public int Capacity{get;set;}
    public DateTime EnrolledAt { get; set; }
    public CourseRecord(string id,  string title,  int capacity, DateTime enrolledAt)
    {
        Id = id;
        Title =title;
        Capacity= capacity;
        EnrolledAt = enrolledAt;
    }

}