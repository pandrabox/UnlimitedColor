Launch liltoon's color correction function from VRC's ExpressionMenu

- Download
    - [VPAI](https://api.anatawa12.com/create-vpai/?name=UnlimitedColor-installer.unitypackage&repo=https://pandrabox.github.io/vpm/index.json&package=com.github.pandrabox.UnlimitedColor&version=%3E=0.0.0)

- これは何？
    - VRCのアバターに使用されているRendererのメインカラーVRC内で色変更できるようにします。Liltoonシェーダーを使っている場合に動作します

- 除外について
    - 本アセットはデフォルト全てのRendererの色を変更可能にしようとします。グループ1つ目、OutOfTargetに登録すると除外されます

- グループについて
    - 名称をつけてRendererをグループ化することができます。グループ化したRendererは同時に色が変化します

- 消費パラメータについて
    - (グループの数+除外していないグループ外Renderer)*4*8bit消費します
    - なるべくグループ化、除外するなどして節約しないとパラメータ上限に容易に接触します
    - VRC FuryにParameter Compressorというものがあり、それを使うと大幅に消費パラメータの節約ができます。多数の色変更を使う場合ご検討下さい

- 色が変わらない（自分視点）
    - Liltoonの機能を使っています。今のところシェーダーを判定せずに自動でメニューを作っているため、Liltoonでないものを操作しようとすると変わりません

- 色が変わらない（他者視点）
    - セーフティにおいてアニメーション・シェーダーなどのカスタム表示がONになっている必要がありますので、その点ご確認下さい

- 名前被りに弱いです。動作がおかしくなるので次のような条件は避けて下さい
    - 同名のグループを定義した場合
    - グループ未定義のRendererといずれかのグループ名が被った場合
    - グループ未定義のRendererの中に名前被りがある場合

- アップロードできない
    - プレハブを消してみて下さい。それで治れば本アセットが原因です
    - 大量のパラメータを使うため、デフォルト状態では多くの場合アップロードできなくなります。上を参照して除外・グループ化等を設定し、どうしても量が多い場合はVRCFuryの導入を検討下さい