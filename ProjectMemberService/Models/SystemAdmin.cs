using System;

namespace ProjectMemberService.Models
{
    public class SystemAdmin
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
