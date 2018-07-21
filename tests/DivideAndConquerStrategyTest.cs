using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using JetBrains.Annotations;

using lib.Commands;
using lib.Models;
using lib.Strategies;
using lib.Utils;

using NUnit.Framework;

namespace tests
{
    [TestFixture]
    public class DivideAndConquerStrategyTest
    {
        [TestCaseSource(nameof(GetModels))]
        [Explicit]
        public void LoadSample([NotNull] string filename)
        {
            Console.WriteLine(filename);
            var content = File.ReadAllBytes(filename);
            var model = Matrix.Load(content);
            var solver = new DivideAndConquer(model);
            try
            {
                solver.Solve();
                File.WriteAllBytes($"C:\\workspace\\icfpc\\res\\{Path.GetFileNameWithoutExtension(filename)}-test.nbt", CommandSerializer.Save(solver.Commands.ToArray()));
            }
            catch
            {
                File.WriteAllBytes($"C:\\workspace\\icfpc\\failed\\{Path.GetFileNameWithoutExtension(filename)}-test.nbt", CommandSerializer.Save(solver.Commands.ToArray()));
                throw;
            }
        }

        private static IEnumerable<TestCaseData> GetModels()
        {
            var problemsDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../data/problemsL");
            foreach (string file in Directory.EnumerateFiles(problemsDir, "*.mdl"))
            {
                yield return new TestCaseData(file).SetName(Path.GetFileNameWithoutExtension(file));
            }
        }
    }
}