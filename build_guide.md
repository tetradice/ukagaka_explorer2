大まかなビルドと動作確認手順
============================

1. gui/GhostExplorer2GUI.sln を Visual Studio で開き、Release構成でビルドする
   (plugin/ghostexplorer2/gui/GhostExplorer2GUI.exe が出力される)

2. plugin/ghostexplorer2 フォルダをSSPで起動中のゴーストにドラッグして、プラグインのnarファイルを作成

3. narからプラグインをインストール



なお、SSPが立ち上がっている状態で、下記のようにコマンドラインオプションを指定して起動すると、
プラグインとしてインストールせず単体起動することもできます。（デバッグ用）

    GhostExplorer2GUI.exe unspecified "C:\ssp\ghost"