using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI.Json
{
    public class VisibleGroups
    {
        public string next_cursor { get; set; }
        public Group[] public_groups { get; set; }
    }
}
