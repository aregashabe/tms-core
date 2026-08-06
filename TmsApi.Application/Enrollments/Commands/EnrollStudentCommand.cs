using MediatR;
using TmsApi.Application.Common;
using TmsApi.Application.Enrollments.Commands;
namespace TmsApi.Application.Enrollments.Commands;
public record EnrollStudentCommand(int StudentId, string CourseCode)
: IRequest<Result<EnrollmentCreated, EnrollmentError>>;
public record EnrollmentCreated(int EnrollmentId, int StudentId, string CourseCode);