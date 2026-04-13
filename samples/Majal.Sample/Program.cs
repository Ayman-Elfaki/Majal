using Majal.Samples;

var person = Empolyee.Create(
    EmployeeName.Create("John"),
    EmpolyeeInformation.Create(
        EmployeePhone.Create("123456789", "US"),
        EmployeeAddress.Create("New York", "USA", "10001")
    )
);

Console.WriteLine(person);