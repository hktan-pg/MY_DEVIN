namespace MyApp.App.Models
{
    /// <summary>
    /// The reminder types offered by the Reminder dialog. The type is stored as text on
    /// <see cref="Reminder"/>, so no lookup table is required.
    /// </summary>
    public static class ReminderTypes
    {
        public const string CarInsurance = "Car Insurance";
        public const string RoadTax = "Road Tax";
        public const string CarService = "Car Service";
        public const string TyreRotation = "Tyre Rotation";
        public const string Battery = "Battery";
        public const string FixedDeposit = "Fixed Deposit";
        public const string Bill = "Bill";
        public const string Subscription = "Subscription";
        public const string Personal = "Personal";
        public const string Other = "Other";

        /// <summary>Every supported reminder type, in the order shown in the dialog.</summary>
        public static IReadOnlyList<string> All { get; } = new[]
        {
            CarInsurance,
            RoadTax,
            CarService,
            TyreRotation,
            Battery,
            FixedDeposit,
            Bill,
            Subscription,
            Personal,
            Other
        };
    }
}
