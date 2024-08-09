# BuildBarTool / 建造栏工具

This mod add a new row to the build menu. MOD developers can bind their new items to the buttons in this new row. 

这个mod添加了一行新的建造栏供mod开发者和玩家使用。

![image.png](https://s2.loli.net/2024/06/29/QnxXb9ZvBe3sYWc.png)

# Examples -- For MOD Developers / 使用示例 -- 给MOD开发者
```
using BuildBarTool;

[BepInPlugin(GUID, NAME, VERSION)] // your mod info 你的mod信息
[BepInDependency(BuildBarToolPlugin.GUID)] // add this mod as hard dependency 将此mod作为你mod的依赖
public class YourPlugin : BaseUnityPlugin
{   
    void Start()
    {
        // ********* Anything you do to register new items. 
		
        BuildBarTool.SetBuildBar(3, 4, 9554, true); 
        // for example, item 9554 will be bound to the button in the red circle (index 4 of top row) in category 3. 
        // "true" means the item will be bound to the top row (red square).
        // 把物品9554绑定到红圈所示的按钮上。 true 代表着绑定在第二行上（红框所示）
    }
	
	// Another way to use this MOD is to use ItemProto.SetBuildBar() after you registered your items
    public static void MethodThatYouRegisterYourItems()
    {
        // ItemProto yourItem;
        // ******** Anything you do to register your items by CommonAPI or LDBTool.
		
        yourItem.SetBuildBar(3, 4, true); 
        // This is another way you can bind your item to the build bar. 你也可以使用这另一种方法。
        // DONNOT USE LDB.items.Select(id) to select an ItemProto to bind build bar. 绝不要使用LDB.items.Select(id)来获取ItemProto进行绑定。
    }
}

```

You can simply set your itemProto's buildIndex = category × 100 + index + **20**, without adding this mod to your mod's hard dependency or `using BuildBarTool;`. In this case, only the players who has installed BuildBarTool will automatically bind that item to the buttons in the top row. Those who doesn't install the BuildBarTool can still play your mod.

你也可以直接把itemProto的buildIndex设置成category × 100 + index + **20**，而不需要`using BuildBarTool;`，也不需要把此mod作为前置mod。若如此做，任何安装了此mod的玩家可以自动将该物品绑定到对应的第二行建造菜单上，未安装此mod的玩家也可以正常使用你的mod。  
```
yourItem.buildIndex = 324; 
// This is another way you can bind your item to the button in the red circle (index 4 of top row) in category 3.
```


## Compatibility -- AND FOR PLAYERS / 兼容性 -- 以及致玩家

This mod is compatible with [RebindBuildBar](https://thunderstore.io/c/dyson-sphere-program/p/kremnev8/RebindBuildBar/), and with that mod, players can customize their own build bar, even for the buttons in the top row.  
此mod与[RebindBuildBar](https://thunderstore.io/c/dyson-sphere-program/p/kremnev8/RebindBuildBar/) 兼容，同时安装这两个mod允许玩家自定义建造菜单（包括第二行）。

## Changelog 更新日志
- v1.0.1
	+ Fixed a compat issue with BuildToolOpt (players could not place white holograms when lacking of item)
	+ 修复了一个和BuildToolOpt的兼容性问题（即使物品不足也可以放置建筑虚影的功能曾无法生效）

- v1.0.0
    + Initial Release
	+ 初始版本
