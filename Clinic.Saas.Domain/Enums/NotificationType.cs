using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Enums
{
    public enum NotificationType : short
    {
        AppointmentReminder = 1,
        FollowUpReminder = 2,
        PaymentReceipt = 3,
        PrescriptionSent = 4,
        SystemAlert = 5,
        LabResult = 6
    }
}
