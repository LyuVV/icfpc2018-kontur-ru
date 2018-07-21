using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using JetBrains.Annotations;

using lib;
using lib.Commands;
using lib.Models;
using lib.Strategies;
using lib.Utils;

using NUnit.Framework;

namespace tests
{
    [TestFixture]
    public class SolveAllTest
    {
        public static int[] Problems = Enumerable.Range(1, 100).ToArray();
        [Test]
        [Explicit]
        //[Timeout(30000)]
        public void Disassemble(
            [Values(2)] int problemId
            //[ValueSource(nameof(Problems))] int problemId
            )
        {
            var problemsDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../data/problemsF");
            var resultsDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../data/solutions");
            var problemFile = Path.Combine(problemsDir, $"FD{problemId.ToString().PadLeft(3, '0')}_src.mdl");
            var matrix = Matrix.Load(File.ReadAllBytes(problemFile));
            var R = matrix.R;
            //var solver = new StupidDisassembler(matrix);
            var assembler = new GreedyPartialSolver(matrix.Voxels, new bool[R, R, R], new Vec(0, 0, 0), new ThrowableHelperFast(matrix));
            var solver = new InvertorDisassembler(assembler);
            List<ICommand> commands = new List<ICommand>();
            try
            {
                var sw = Stopwatch.StartNew();
                commands.AddRange(solver.Solve());
            }
            catch (Exception e)
            {
                Log.For(this).Error($"Unhandled exception in solver for {Path.GetFileName(problemFile)}", e);
            }
            finally
            {
                var bytes = CommandSerializer.Save(commands.ToArray());
                File.WriteAllBytes(GetSolutionPath(resultsDir, problemFile), bytes);
            }
        }
        [Test]
        [Explicit]
        //[Timeout(30000)]
        public void SolveOne(
            [Values(1)] int problemId
            //[ValueSource(nameof(Problems))] int problemId
            )
        {
            var problemsDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../data/problemsF");
            var resultsDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../data/solutions");
            var problemFile = Path.Combine(problemsDir, $"FA{problemId.ToString().PadLeft(3, '0')}_tgt.mdl");
            var matrix = Matrix.Load(File.ReadAllBytes(problemFile));
            var R = matrix.R;
            //var solver = new GreedyPartialSolver(matrix.Voxels, new bool[R, R, R], new Vec(0, 0, 0), new ThrowableHelper(matrix), new BottomToTopBuildingAround());
            var solver = new GreedyPartialSolver(matrix.Voxels, new bool[R, R, R], new Vec(0, 0, 0), new ThrowableHelper(matrix), new NearToFarBottomToTopBuildingAround());
            //var solver = new DivideAndConquer(matrix);
            List<ICommand> commands = new List<ICommand>();
            try
            {
                var sw = Stopwatch.StartNew();
                commands.AddRange(solver.Solve().TakeWhile(x => sw.Elapsed.TotalSeconds < 20));
                Console.WriteLine(GreedyPartialSolver.candidatesCount.ToDetailedString());
            }
            catch (Exception e)
            {
                Log.For(this).Error($"Unhandled exception in solver for {Path.GetFileName(problemFile)}", e);
            }
            finally
            {
                var bytes = CommandSerializer.Save(commands.ToArray());
                File.WriteAllBytes(GetSolutionPath(resultsDir, problemFile), bytes);
            }

            var solutionEnergy = GetSolutionEnergy(matrix, commands.ToArray(), problemFile);
            Console.WriteLine(solutionEnergy);
            Console.WriteLine(ThrowableHelper.opt.ToDetailedString());
        }

        [Test]
        public void Solve()
        {
            var problemsDir =  FileHelper.ProblemsDir;
            var resultsDir = FileHelper.SolutionsDir;
            var allProblems = Directory.EnumerateFiles(problemsDir, "*.mdl");
            var problems = allProblems.Select(p =>
                {
                    var matrix = Matrix.Load(File.ReadAllBytes(p));
                    return new {m = matrix, p, weight = matrix.Voxels.Cast<bool>().Count(b => b)};
                }).Take(10).ToList();
            problems.Sort((p1, p2) => p1.weight.CompareTo(p2.weight));
            Log.For(this).Info(string.Join("\r\n", problems.Select(p => p.p)));
            Parallel.ForEach(problems, p =>
                {
                    var R = p.m.R;
                    //var solver = new GreedyPartialSolver(p.m.Voxels, new bool[R, R, R], new Vec(0, 0, 0), new ThrowableHelper(p.m));
                    var solver = new GreedyPartialSolver(p.m.Voxels, new bool[R, R, R], new Vec(0, 0, 0), new ThrowableHelperFast(p.m));
                    //var solver = new DivideAndConquer(p.m);
                    //var solverName = "div-n-conq";
                    var solverName = "greedy-fst";
                    ICommand[] commands = null;
                    try
                    {
                        commands = solver.Solve().ToArray();
                    }
                    catch (Exception e)
                    {
                        Log.For(this).Error($"Unhandled exception in solver for {Path.GetFileName(p.p)}", e);
                        return;
                    }

                    var solutionEnergy = GetSolutionEnergy(p.m, commands, p.p);

                    var bytes = CommandSerializer.Save(commands);
                    File.WriteAllBytes(GetSolutionPath(resultsDir, p.p, solverName, solutionEnergy), bytes);
                });
        }

        private static string GetSolutionPath(string resultsDir, string p)
        {
            return Path.Combine(resultsDir, GetSolutionFilename(p));
        }

        private static string GetSolutionPath(string resultsDir, string p, string solverName, long solutionEnergy)
        {
            return Path.Combine(resultsDir, GetSolutionFilename(p, solverName, solutionEnergy));
        }

        private static string GetSolutionFilename(string problemName, string solverName, long solutionEnergy)
        {
            return $"{Path.GetFileNameWithoutExtension(problemName).Split('_')[0]}-{solverName}-{solutionEnergy}.nbt";
        }

        private static string GetSolutionFilename(string p)
        {
            return $"{Path.GetFileNameWithoutExtension(p).Split('_')[0]}.nbt";
        }

        private long GetSolutionEnergy(Matrix problem, ICommand[] commands, string p)
        {
            try
            {
                return Validate(problem, commands);
            }
            catch (Exception e)
            {
                Log.For(this).Error($"Invalid solution for {Path.GetFileName(p)}", e);
                throw;
            }
        }

        private long Validate([NotNull] Matrix problem, [NotNull] ICommand[] solution)
        {
            var state = new DeluxeState(null, problem);
            new Interpreter(state).Run(solution);
            return state.Energy;
        }
    }
}