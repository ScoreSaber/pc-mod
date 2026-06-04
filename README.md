<p align="center">
   <img src="https://scoresaber.com/ScoreSaber-iOS-Default-1024x1024@1x.png" title="ScoreSaber" alt="ScoreSaber icon" width="96" />
</p>

<h1 align="center">PC Mod</h1>

The [BSIPA](https://github.com/nike4613/BeatSaber-IPA-Reloaded) plugin for ScoreSaber on PC 

## Local Build Settings

If you want to be able to upload scores from a dev build of ScoreSaber (without being rude about it) you're going to need a dev token. Feel free to contact one of our admins for one. You can find their social contact information [here](https://scoresaber.com/team) of if emails more your thing, here ya go: developers@scoresaber.com

For dev only upload trust secrets, copy `Directory.Build.local.props.example` to `Directory.Build.local.props`

Useful local properties:

- `ScoreSaberDevelopmentUploadToken`: local upload trust dev token for testing protocol v2 auth/uploads
- `ScoreSaberOfficialBuildId` / `ScoreSaberOfficialBuildCredential`: official build metadata, normally supplied by CI

You probably don't have they key to use this and is only used in extreme circumstances. Allows admins to bypass platform authentication methods.
- `ScoreSaberDevelopmentAuthNonce`: local auth type 3 nonce
- `ScoreSaberDevelopmentPlayerId` / `ScoreSaberDevelopmentPlayerName`: optional local identity override for auth type 3

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for code standards and pull request expectations
