namespace Runtira.Application.Common
{
    public sealed class OrganizationAccessOptionDto
    {
        public Guid OrganizationId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public bool IsDemo { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
