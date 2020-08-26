namespace Hashset.Wednesday

open Microsoft.Azure.WebJobs
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open FsHttp
open FsHttp.DslCE

open FSharp.Data

//*/20 * * * * *

type Pleroma = JsonProvider<"./sampleStatuses.json">

module TootTimer =
    // Automagically translated using Deepl.
    let messages =
        [| "It is #Wednesday, my dudes"
           "Es ist #Mittwoch, meine Kerle"
           "C'est #mercredi, mes amis"
           "Es #miércoles, mis amigos"
           "E' #mercoledì, amici miei"
           "Het is #woensdag, mijn jongens"
           "Jest #środa, kolesie"
           "Сегодня #среда, чуваки" |]

    [<FunctionName("WednesdayToot")>]
    let run([<TimerTrigger("0 0 12 * * 3")>]myTimer: TimerInfo, log: ILogger) =
        let config = ConfigurationBuilder()
                         .AddJsonFile("local.settings.json", optional = true, reloadOnChange = true)
                         .AddEnvironmentVariables()
                         .Build()

        let lastMessage =
            http {
                GET "https://fedi.absturztau.be/api/v1/accounts/wednesdaybot/statuses"
            }
            |> toText
            |> Pleroma.Parse
            |> Seq.head
            |> fun s -> s.Content

        let messagesIndex (lastMessage: string) =
            messages
            |> Array.tryFindIndex (fun m ->
                let start = lastMessage.Substring(0, lastMessage.IndexOf("<"))
                m.StartsWith(start)
            )
            |> Option.map (fun i -> if i + 1 < messages.Length then i + 1 else 0)
            |> Option.defaultValue 0

        http {
            POST "https://fedi.absturztau.be/api/v1/statuses"
            Authorization config.["BASIC_AUTH"]
            body
            ContentType "application/x-www-form-urlencoded"
            formUrlEncoded [
                "status", sprintf """%s




---
This is a bot. See a mistake or want to add your own language? Submit an issue or PR here: https://github.com/kaeedo/WednesdayBot
                """ messages.[messagesIndex lastMessage]
            ]
        } |> ignore

        ()
