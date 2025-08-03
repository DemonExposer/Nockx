# Nockx™
<img src="nockx-logo.png" width="400px" align="right"/>
Nockx™ (like Fort Knox) is a secure chat application.
It uses 256-bit AES encryption to exchange messages and audio. AES, being a symmetric algorithm, can only be used with both the sender and the receiver having the same key. For this reason, the AES key, for each message or voice call, is encrypted using a 2048-bit RSA key, unique to each user. Messages and requests are also verified with the RSA key to ensure data integrity.<br/>
Messages are stored on a server (closed-source), to make sure that the messages and keys are separated.

## Important information about the architecture
AES-CBC is used for encryption. Since this is vulnerable to the padding oracle attack, AES encrypted data should NEVER be handled by the server.
This is why the server is like a "Chinese room", just sending the data straight from the sender to the receiver. This is a side-effect of the server not being allowed to know the plaintext in the first place.
Otherwise, the whole architecture wouldn't be secure.
That this holds true can be observed by the design of the client, no one can make a server which is able to read the plaintext, because the client never encrypts it with a key received from the server. We know the keys are not sent from the server itself, because when they're sent, they are signed with the sender's RSA key.
<strong>SO PLEASE DO CHECK WHETHER THE SENDER'S PUBLIC KEY IS THE ONE THAT YOU EXPECT IT TO BE</strong>.

However, there is still one issue. If an account is run by a bot, this bot could check whether the ciphertext's padding is correct and send back a message based on this.
This way, a padding oracle attack could be performed and the message sent by anyone to this bot could be decrypted. <strong>THIS IS WHY MESSAGES SHOULD BE DISCARDED IF THE RSA SIGNATURE IS NOT CORRECT AND NO FEEDBACK SHOULD BE GIVEN ON WHETHER THE CIPHERTEXT IS CORRECT. ALSO, EVERY TIME PLAINTEXT IS SENT FOR A RECEIVER TO READ, THE PLAINTEXT HAS TO BE SIGNED WITH AN RSA SIGNATURE.</strong>
In Nockx™, all of this holds true, the messages are simply discarded if anything is incorrect and the signature is always checked against the plaintext (except in calls, <strong>SO DO NOT CALL WITH BOTS WHICH RESPOND TO VOICE INPUT</strong>). This has to stay this way.<br/>
If anyone wants to change this behavior, first of all, don't. Second, if you are stubborn enough to do it, do not run a bot on such a modified client. With a human, such subtle timing differences in responses can't be observed, but with bots, it can.<br/>

In the end, if someone does run a bot which gives back a message when it receives incorrect ciphertext, only your messages to that bot can be decrypted.
This is because a different AES key is used for each message. So, it's not that big of an issue.<br/>
It also poses the following question: why would you send sensitive information to a bot in the first place? Please don't.

<br/>
<br/>

<strong>Note: because Nockx™ is still in pre-release, the entire aforementioned description MIGHT NOT BE CORRECT. This is only an indication of how the software will function when it is released!</strong>
