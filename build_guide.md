大まかなビルドと動作確認手順
============================

1. gui/GhostExplorer2GUI.sln を Visual Studio で開き、Release構成でビルドする
   (plugin/ghostexplorer2/gui/GhostExplorer2GUI.exe が出力される)

2. plugin_src/ghostexplorer2/ghostexplorer2.sln を Visual Studio で開き、Release構成でビルドする
   (plugin/ghostexplorer2/module/ フォルダ以下にいくつかのdllが出力される)

3. plugin/ghostexplorer2 フォルダをSSPで起動中のゴーストにドラッグして、プラグインのnarファイルを作成

4. narからプラグインをインストール



単体でデバッグ起動する際には、下記のようにコマンドライン引数として
"unspecified", およびSSPのフォルダパスを渡してください。
（1つめの引数は、呼び出し元のゴーストを表します。unspecifiedで指定なし）

    コマンドライン引数: unspecified "C:\ssp"
    
なお、SSPが起動している状態であれば、SSPのフォルダパスは省略できます。

    コマンドライン引数: unspecified