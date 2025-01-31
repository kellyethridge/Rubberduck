using System.Runtime.InteropServices;
using NLog;
using Rubberduck.Parsing.VBA;
using Rubberduck.VBEditor.SafeComWrappers;
using Rubberduck.VBEditor.SafeComWrappers.Abstract;
using Rubberduck.Parsing.Symbols;
using Rubberduck.UI.Command;
using Rubberduck.UnitTesting.CodeGeneration;

namespace Rubberduck.UI.UnitTesting.Commands
{
    /// <summary>
    /// A command that adds a new test module to the active VBAProject.
    /// </summary>
    [ComVisible(false)]
    public class AddTestModuleCommand : CommandBase
    {
        private readonly RubberduckParserState _state;
        private readonly ITestCodeGenerator _codeGenerator;

        public AddTestModuleCommand(IVBE vbe, RubberduckParserState state, ITestCodeGenerator codeGenerator)
            : base(LogManager.GetCurrentClassLogger())
        {
            Vbe = vbe;
            _state = state;
            _codeGenerator = codeGenerator;
        }

        protected IVBE Vbe { get; }

        private IVBProject GetProject()
        {
            //No using because the wrapper gets returned potentially. 
            var activeProject = Vbe.ActiveVBProject;
            if (!activeProject.IsWrappingNullReference)
            {
                return activeProject;
            }
            activeProject.Dispose();
            
            using (var projects = Vbe.VBProjects)
            {
                return projects.Count == 1
                    ? projects[1] // because VBA-Side indexing
                    : null;
            }
        }

        protected override bool EvaluateCanExecute(object parameter)
        {
            bool canExecute;
            using (var project = GetProject())
            {
                canExecute = project != null && !project.IsWrappingNullReference && CanExecuteCode(project);
            }

            return canExecute;
        }
        
        private bool CanExecuteCode(IVBProject project)
        {
            return project.Protection == ProjectProtection.Unprotected;
        }

        protected override void OnExecute(object parameter)
        {
            var parameterIsModuleDeclaration = parameter is ProceduralModuleDeclaration || parameter is ClassModuleDeclaration;

            switch(parameter)
            {
                case IVBProject project:
                    _codeGenerator.AddTestModuleToProject(project);
                    break;
                case Declaration declaration when parameterIsModuleDeclaration:
                    _codeGenerator.AddTestModuleToProject(declaration.Project, declaration);
                    break;
                default:
                    using (var project = GetProject())
                    {
                        _codeGenerator.AddTestModuleToProject(project, null);
                    }
                    break;
            }

            _state.OnParseRequested(this);
        }
    }
}