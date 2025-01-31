﻿using Rubberduck.Inspections.Abstract;
using System.Collections.Generic;
using System.Linq;
using Rubberduck.Parsing.Inspections.Abstract;
using Rubberduck.Resources.Inspections;
using Rubberduck.Parsing.VBA;
using Rubberduck.Parsing.Grammar;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime;
using Rubberduck.Parsing;
using Rubberduck.VBEditor;
using Rubberduck.Inspections.Results;
using static Rubberduck.Parsing.Grammar.VBAParser;

namespace Rubberduck.Inspections.Concrete
{
    public sealed class StepIsNotSpecifiedInspection : ParseTreeInspectionBase
    {
        public StepIsNotSpecifiedInspection(RubberduckParserState state) : base(state) { }

        protected override IEnumerable<IInspectionResult> DoGetInspectionResults()
        {
            return Listener.Contexts
                .Where(result => !IsIgnoringInspectionResultFor(result.ModuleName, result.Context.Start.Line))
                .Select(result => new QualifiedContextInspectionResult(this,
                                                        InspectionResults.StepIsNotSpecifiedInspection,
                                                        result));
        }

        public override IInspectionListener Listener { get; } =
            new StepIsNotSpecifiedListener();
    }

    public class StepIsNotSpecifiedListener : VBAParserBaseListener, IInspectionListener
    {
        private readonly List<QualifiedContext<ParserRuleContext>> _contexts = new List<QualifiedContext<ParserRuleContext>>();
        public IReadOnlyList<QualifiedContext<ParserRuleContext>> Contexts => _contexts;

        public QualifiedModuleName CurrentModuleName
        {
            get;
            set;
        }

        public void ClearContexts()
        {
            _contexts.Clear();
        }

        public override void EnterForNextStmt([NotNull] ForNextStmtContext context)
        {
            StepStmtContext stepStatement = context.stepStmt();

            if (stepStatement == null)
            {
                _contexts.Add(new QualifiedContext<ParserRuleContext>(CurrentModuleName, context));
            }
        }
    }
}
