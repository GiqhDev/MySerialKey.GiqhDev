using System.Security.Cryptography;
using MySerialKey.GiqhDev.Generation;
using MySerialKey.GiqhDev.Validation;

string serial = SerialKeyGenerator.Generate();
SerialValidationResult formatResult = SerialKeyValidator.Validate(serial);

Console.WriteLine($"Generated serial passes format validation: {formatResult.IsValid}");

byte[] secretKey = RandomNumberGenerator.GetBytes(32);
string authenticatedSerial = SerialKeyGenerator.GenerateAuthenticated(secretKey);
SerialValidationResult authenticationResult = SerialKeyValidator.ValidateAuthenticated(authenticatedSerial, secretKey);

Console.WriteLine($"Generated authenticated serial is valid: {authenticationResult.IsValid}");
CryptographicOperations.ZeroMemory(secretKey);
