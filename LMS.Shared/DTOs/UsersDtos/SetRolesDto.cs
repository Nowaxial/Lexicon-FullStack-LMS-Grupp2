using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Shared.DTOs.UsersDtos
{
    public class SetRolesDto
    {
        public required IEnumerable<string> Roles { get; init; }
    }

}
