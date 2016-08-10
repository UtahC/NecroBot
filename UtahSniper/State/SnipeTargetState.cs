using PoGo.NecroBot.Logic.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using PoGo.NecroBot.Logic.Tasks;

namespace UtahSniper.State
{
    class SnipeTargetState : IState
    {
        public async Task<IState> Execute(ISession session, CancellationToken cancellationToken)
        {
            await SnipeTargetTask.Execute(session, cancellationToken);
            return null;
        }
    }
}
