using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vosk;
using Newtonsoft.Json.Linq;
using NAudio.Wave;

namespace VoskApi.Controllers
{
    [ApiController]
    [Route("api/speech-to-text")]
    public class SpeechToTextController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly Model _model;

        public SpeechToTextController(IWebHostEnvironment environment)
        {
            _environment = environment;
            _model = new Model(Path.Combine(_environment.ContentRootPath, "model"));
        }

        public IActionResult Get() {
            return Ok("Welcome to VoskApi!");
        }

        [HttpPost]
        public IActionResult Post(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Please provide an audio file");
                }

                // Create a recognizer object with the provided model
                VoskRecognizer rec = new VoskRecognizer(_model, 16000.0f);
                rec.SetMaxAlternatives(0);
                rec.SetWords(true);

                // Read the audio file into a byte buffer
                using (MemoryStream ms = new MemoryStream())
                {
                    file.CopyTo(ms);
                    byte[] buffer = ms.ToArray();

                    // Recognize the speech from the buffer
                    if (rec.AcceptWaveform(buffer, buffer.Length))
                    {
                        // string result = rec.Result();
                        // return Ok(result);
                        RecognitionResult result = new RecognitionResult(rec.Result());
                        return Ok(new { text = result.Text, confidence = result.Confidence });
                    }
                    else
                    {
                        // string result = rec.PartialResult();
                        // return Ok(result);
                        RecognitionResult result = new RecognitionResult(rec.PartialResult());
                        return Ok(new { text = result.Text, confidence = result.Confidence });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception " + ex.Message);
                return BadRequest(ex.Message);
            }
        }
    }

    public class RecognitionResult
    {
        public string Text { get; set; }
        public double Confidence { get; set; }

        public RecognitionResult(string resultJson)
        {
            JObject obj = JObject.Parse(resultJson);
            
            if(obj == null)
                throw new Exception("Unparsable Json.");

            Text = obj["text"]?.ToString();

            if(Text == null)
                throw new Exception("Unparsable Json.");

            JArray result = (JArray)obj["result"];
            double totalConf = 0.0;
            int numResults = result.Count;

            foreach (JObject resultItem in result)
            {
                double conf = (double?)resultItem["conf"] ?? 0;
                totalConf += conf;
            }

            double avgConf = totalConf / numResults;
            Confidence = avgConf;
        }
    }
}