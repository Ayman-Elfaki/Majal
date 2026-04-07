using Majal.Samples;

var person = Empolyee.Create(
    EmpolyeeName.Create("John"),
    EmpolyeeInformation.Create(
        EmpolyeePhone.Create("123456789", "US"),
        EmpolyeeAddress.Create("New York", "USA", "10001")
    )
);

Console.WriteLine(person);