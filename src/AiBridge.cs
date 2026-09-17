using System;
using System.Text;
using System.Threading;
namespace Miareve {
    // Optional dedicated CLI bridge: stdin JSON -> Ollama HTTP -> stdout JSON.
    public static class AiBridge {
        public static int Main(string[] args) {
            Console.InputEncoding=Encoding.UTF8; Console.OutputEncoding=new UTF8Encoding(false);
            try {
                var settings=new Settings();
                for(int i=0;i<args.Length;i+=2) {
                    if(i+1>=args.Length) throw new ArgumentException("Expected --endpoint URL or --model NAME");
                    if(args[i]=="--endpoint") settings.Endpoint=args[i+1];
                    else if(args[i]=="--model") settings.Model=args[i+1];
                    else throw new ArgumentException("Unknown argument");
                }
                string line=Console.ReadLine(); if(line==null || line.Length>8000000) throw new ArgumentException("Expected one bounded JSON input line");
                var input=Json.Read<AiInput>(line); settings.Keywords=input.keywords??"";
                if(input.images==null || input.images.Length<1 || input.images.Length>2) throw new ArgumentException("Expected 1–2 base64 JPEG images");
                using(var cts=new CancellationTokenSource(TimeSpan.FromSeconds(110))) {
                    var result=AiClient.Classify(settings,input.title??"",input.images,cts.Token).GetAwaiter().GetResult();
                    Console.WriteLine(Json.Write(result));
                }
                return 0;
            } catch(Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
        }
    }
}
