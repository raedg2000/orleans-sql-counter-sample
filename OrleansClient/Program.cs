using System.Net.Http.Json;

Guid Counter1 = new("9ec79b44-967b-4684-87ab-1b3f919054a7");
Guid Counter2 = new("a0eff5e9-fa0d-471e-9100-c5f9335d6051");
Guid Counter3 = new("83519673-3e02-4aa2-98f6-e51207d242ef");
Guid Counter4 = new("1651f451-4519-4732-8029-3b360c0226b2");
Guid Counter5 = new("0268cc26-4ad4-4cae-8b9f-8379d3db2979");

Console.WriteLine("Starting Orleans Client...");

var apiBaseUrl = "http://localhost:5131";
var counterId = Counter1;

using var httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

var incrementResponse = await httpClient.PostAsync($"counter/{counterId}/increment", null);
incrementResponse.EnsureSuccessStatusCode();
int result = await incrementResponse.Content.ReadFromJsonAsync<int>();

var getResponse = await httpClient.GetAsync($"counter/{counterId}");
getResponse.EnsureSuccessStatusCode();
int value = await getResponse.Content.ReadFromJsonAsync<int>();

Console.WriteLine($"Last Increment result: {result}");
Console.WriteLine($"CurrentCounter value: {value}");
Console.WriteLine();

Console.ReadLine();
