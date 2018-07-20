using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using lib.Models;

using Microsoft.AspNetCore.Mvc;

namespace ui.Controllers
{
    [Route("api/[controller]")]
    public class MatrixController : Controller
    {
        [HttpGet("[action]")]
        public string[,,] Index(int i)
        {
            const string problemsDir = "../data/problemsL";
            var filename = Directory.EnumerateFiles(problemsDir, "*.mdl").ToList()[i];

            if (filename == null) return null;

            var content = System.IO.File.ReadAllBytes(filename);

            var voxels = Matrix.Load(content).Voxels;
            var strings = new string[voxels.GetLength(0), voxels.GetLength(1), voxels.GetLength(2)];

            for (var x = 0; x < voxels.GetLength(0); x++)
            {
                for (var y = 0; y < voxels.GetLength(1); y++)
                {
                    for (var z = 0; z < voxels.GetLength(2); z++)
                    {
                        strings[x, y, z] = voxels[x, y, z] ? "" : "0";
                    }
                }
            }

            return strings;
        }
    }
}