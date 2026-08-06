namespace TmsApi.Application.Enrollments.Commands;

public record EnrollmentError(
    string Code,
    string Message)
{
    public static EnrollmentError CourseNotFound(string courseCode)
    {
        return new EnrollmentError(
            "course_not_found",
            $"Course with code '{courseCode}' was not found.");
    }


    public static EnrollmentError CourseFull(
        string courseTitle,
        int maxCapacity)
    {
        return new EnrollmentError(
            "course_full",
            $"Course '{courseTitle}' has reached maximum capacity of {maxCapacity}.");
    }


    public static EnrollmentError AlreadyEnrolled(
        int studentId,
        string courseCode)
    {
        return new EnrollmentError(
            "already_enrolled",
            $"Student {studentId} is already enrolled in course '{courseCode}'.");
    }
}