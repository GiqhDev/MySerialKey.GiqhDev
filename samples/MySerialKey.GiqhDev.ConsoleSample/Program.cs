using System.Text;
using MySerialKey.GiqhDev;

string serial = SerialKeyGenerator.Generate();
SerialValidationResult formatResult = SerialKeyValidator.Validate(serial);

Console.WriteLine(serial);
Console.WriteLine(formatResult.IsValid);

byte[] secretKey = Encoding.UTF8.GetBytes("sample-secret-key-change-me");
string authenticatedSerial = SerialKeyGenerator.GenerateAuthenticated(secretKey);
SerialValidationResult authenticationResult = SerialKeyValidator.ValidateAuthenticated(authenticatedSerial, secretKey);

Console.WriteLine(authenticatedSerial);
Console.WriteLine(authenticationResult.IsValid);
