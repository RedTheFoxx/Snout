using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snout.CoreDeps;
internal class LiveHandlers
{
    
    /* This class is used to handle the event monitoring hooked in the Main() method.
     * 
     * IMPORTANT : Keep this comment updated with the latest changes in DB and new actions declared.
     * 
     * Declared actions in database :
     * 
     * action_MESSAGE_SENT
     * action_TAGUED_BY
     * action_JOINED_VOCAl
     * action_CHANGED_STATUS
     * action_MESSAGE_SENT_WITH_FILE
     * action_TAGUED_SOMEONE
     * action_USED_SNOUT_COMMAND
     * 
     * Each function is used to handle an event but its scope is not limited to the Paycheck modules, it can be reused for future things.
     * 
     */
}
