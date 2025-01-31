using System.Collections.Generic;
using Rubberduck.Inspections.Abstract;
using Rubberduck.Parsing.Inspections.Abstract;
using Rubberduck.Parsing.VBA;
using Rubberduck.Inspections.CodePathAnalysis;
using Rubberduck.Parsing.Symbols;
using Rubberduck.Inspections.CodePathAnalysis.Extensions;
using System.Linq;
using Rubberduck.Inspections.Results;
using Rubberduck.Parsing.Grammar;

namespace Rubberduck.Inspections.Concrete
{
    public sealed class AssignmentNotUsedInspection : InspectionBase
    {
        private readonly Walker _walker;

        public AssignmentNotUsedInspection(RubberduckParserState state, Walker walker)
            : base(state) {
            _walker = walker;
        }

        protected override IEnumerable<IInspectionResult> DoGetInspectionResults()
        {
            var variables = State.DeclarationFinder
                    .UserDeclarations(DeclarationType.Variable)
                    .Where(d => !d.IsArray);

            var nodes = new List<IdentifierReference>();
            foreach (var variable in variables)
            {
                var parentScopeDeclaration = variable.ParentScopeDeclaration;

                if (parentScopeDeclaration.DeclarationType.HasFlag(DeclarationType.Module))
                {
                    continue;
                }

                var tree = _walker.GenerateTree(parentScopeDeclaration.Context, variable);

                var references = tree.GetIdentifierReferences();
                // ignore set-assignments to 'Nothing'
                nodes.AddRange(references.Where(r =>
                    !(r.Context.Parent is VBAParser.SetStmtContext setStmtContext &&
                    setStmtContext.expression().GetText().Equals(Tokens.Nothing))));
            }

            return nodes
                .Select(issue => new IdentifierReferenceInspectionResult(this, Description, State, issue))
                .ToList();
        }
    }
}
