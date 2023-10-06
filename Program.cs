using System.Text;
using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

var models = new List<Model>();
models.Add(new Model("1", "anthropic.claude-instant-v1", "{{'prompt': 'Human:{0} Assistant:', 'max_tokens_to_sample':300, 'temperature':1, 'top_k':250,'top_p':0.999, 'stop_sequences':['Human'],'anthropic_version':'bedrock-2023-05-31'}}"));
models.Add(new Model("2", "anthropic.claude-v1", "{{'prompt': 'Human:{0} Assistant:', 'max_tokens_to_sample':300, 'temperature':1, 'top_k':250, 'top_p':1, 'stop_sequences':['Human'],'anthropic_version':'bedrock-2023-05-31' }}"));
models.Add(new Model("3", "anthropic.claude-v2", "{{'prompt': 'Human:{0} Assistant:', 'temperature':0.5, 'top_p':1 , 'max_tokens_to_sample':300, 'top_k':250,'stop_sequences':['Human'] }}"));
models.Add(new Model("4", "ai21.j2-ultra-v1", "{{'prompt':'{0}','maxTokens':200,'temperature':0.7,'topP':1,'stopSequences':[],'countPenalty':{{'scale':0}},'presencePenalty':{{'scale':0}},'frequencyPenalty':{{'scale':0}}}}"));
models.Add(new Model("5", "ai21.j2-mid-v1", "{{'prompt':'{0}','maxTokens':200,'temperature':0.7,'topP':1,'stopSequences':[],'countPenalty':{{'scale':0}},'presencePenalty':{{'scale':0}},'frequencyPenalty':{{'scale':0}}}}"));
models.Add(new Model("6", "cohere.command-text-v14", "{{'prompt':'{0}','max_tokens':400,'temperature':0.75, 'p':0.01, 'k':0, 'stop_sequences':[], 'return_likelihoods': 'NONE'}}"));

// Command line processing

if (args.Length < 2)
{
    Console.WriteLine("Generate text with Amazon Bedrock.");
    Console.WriteLine(@"Usage: dotnet run -- model ""prompt""");
    Console.WriteLine("Available models:");
    foreach (var m in models)
    {
        Console.WriteLine($"{m.Nickname} {m.Id}");
    }
    Environment.Exit(0);
}

var model = models.Where(m =>m.Nickname == args[0] || m.Id == args[0]).FirstOrDefault();
if (model==null)
{
    Console.WriteLine($"Model {args[0]} not found");
    Environment.Exit(0);
}

var prompt = args[1];
prompt = prompt.Replace("'", "\\'");

Console.WriteLine("Model: " + model.Id);

// Create the request.

AmazonBedrockRuntimeClient client = new AmazonBedrockRuntimeClient(new AmazonBedrockRuntimeConfig
{
    RegionEndpoint = RegionEndpoint.USWest2
});

InvokeModelRequest request = new InvokeModelRequest();
request.ModelId = model.Id;

JObject json = JObject.Parse(String.Format(model.Body, prompt));
byte[] byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(json));
MemoryStream stream = new MemoryStream(byteArray);
request.Body = stream;
request.ContentType = "application/json";
request.Accept = "application/json";

// Invoke model and output the response.

var response = await client.InvokeModelAsync(request);
string responseBody = new StreamReader(response.Body).ReadToEnd();
dynamic parseJson = JsonConvert.DeserializeObject(responseBody);
string answer = model.Id switch
{
    "anthropic.claude-instant-v1" => parseJson?.completion,
    "anthropic.claude-v1" => parseJson?.completion,
    "anthropic.claude-v2" => parseJson?.completion,
    "ai21.j2-mid-v1" => parseJson?.completions[0].data.text,
    "ai21.j2-ultra-v1" => parseJson?.completions[0].data.text,
    "cohere.command-text-v14" => parseJson?.generations[0].text,
    _ => null
};

Console.WriteLine(answer?.Trim());
Environment.Exit(0);

class Model
{
    public string Nickname;
    public string Id;
    public string Body;
    public Model(string nickname, string id, string body) { Nickname = nickname; Id = id; Body = body; }
}