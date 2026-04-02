namespace ChatCore.Models;

public enum MessageType
{
    Message,         // Plain message (MSG)
    MachineRequest,  // Machine asks pilot something (MR_Req)
    MachineResponse, // Machine responds to pilot (MR_Rec)
    PilotRequest,    // Pilot asks machine something (PR_Req)
    PilotResponse,   // Pilot responds to machine (PR_Rec)
    Ack              // Delivery acknowledgment (never shown in UI)
}
