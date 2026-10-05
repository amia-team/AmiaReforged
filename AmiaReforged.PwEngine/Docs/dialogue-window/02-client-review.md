# Step 2 — Client behavior review

Status: **Pending deployment and in-game checks.**
Parent stage: [Step 2 — Events](02-events.md).

Deploy the updated PwEngine and start a normal custom conversation with an NPC.
`./dialogueui` remains an isolated visual preview and does not exercise dialogue
advancement. The normal conversation now opens the approved graphical shell.

1. **Responses and branches:** choose a response, follow a branch and a Continue
   response, and reach a terminal node. Check that conditions, actions and rewards
   behave as before. Verify a speaker change updates both headings and the portrait.
2. **Pagination:** use text previous/next and More on a node with more than five
   responses. Text pages and response pages must change independently. Check
   disabled arrows at the first/last text page, More wraparound, and hidden slots
   on the last response page. Select a response from that last page.
3. **Mouse input:** rapidly press a response, right-click it, and press then
   release elsewhere. Only a matching left press/release should advance, once.
   During an asynchronous action, navigation and response controls must be disabled.
   A held press must not select a replacement response after a refresh/page change.
4. **Ending and reopening:** end once with Goodbye and once with the red X.
   Reopen the conversation each time; the NPC must be available again. Confirm
   a terminal node's farewell remains visible when the existing runtime requires it.
5. **Lifecycle:** walk more than five meters away, disconnect during a conversation,
   and remove a participant on a development server. Reconnect/reopen as applicable;
   no session or NPC-busy state should remain. Check that ending during a pending
   refresh/action cannot revive the old window.

Record failures with the dialogue/tree/node involved and the triggering action.
Record successful native results in [Step 2 completion evidence](02-events.md#completion-evidence).
