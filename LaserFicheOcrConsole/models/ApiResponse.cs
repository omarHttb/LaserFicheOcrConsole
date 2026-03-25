using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserFicheOcrConsole.models
{
    public class ApiResponse
    {
        public string doc_id { get; set; } = string.Empty;
        public string filename { get; set; } = string.Empty;
        public string content_type { get; set; } = string.Empty;
        public string markdown_content { get; set; } = string.Empty;

        public static implicit operator string(ApiResponse v)
        {
            throw new NotImplementedException();
        }
    }
}
