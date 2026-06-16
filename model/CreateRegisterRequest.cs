public record CreateRegisterRequest(
    string StudentId,
    string Name,
    int Age,
    decimal? GPA
);