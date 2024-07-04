using xiaoye97;

namespace BuildBarTool
{
    public static class BuildBarTool
    {
        /// <summary>
        /// Bind an item to a button on the build bar. 将一个物品绑定到建造栏的按钮上。
        /// </summary>
        /// <param name="itemId">Id of the item you want to bind. 要绑定的物品Id。</param>
        /// <param name="category">Category that the button belongs to (1-10). 按钮所在的建造栏的类别序号（1-10）。</param>
        /// <param name="index">Button position from left to right (1-10). 按钮位置序号（从左向右1-10）。</param>
        /// <param name="isTopRow">Bind the item to the buttons in top row (true) or bottom row (false). 将建筑绑定在上面一行(truw)还是下面一行(false)的按钮上。</param>
        /// <returns></returns>
        public static bool SetBuildBar(int category, int index, int itemId, bool isTopRow)
        {
            if(category >= 1 && category <= 12)
            {
                if (index >= 1 && index <= 10)
                {
                    if (!isTopRow)
                    {
                        LDBTool.SetBuildBar(category, index, itemId);
                    }
                    else if (isTopRow)
                    {
                        if (BuildBarToolPlugin.protoIds[category, index] > 0)
                        {
                            BuildBarToolPlugin.logger.LogWarning(string.Format("Bind Build Bar Fail (item ID:{0}). Build bar [{1}, tier2, {2}] is already bound with another item ID:{3}.", new object[]
                            {
                                itemId,
                                category,
                                index,
                                BuildBarToolPlugin.protoIds[category, index]
                            }));
                            return false;
                        }
                        else
                            BuildBarToolPlugin.protoIds[category, index] = itemId;
                    }
                    else
                    {
                        BuildBarToolPlugin.logger.LogWarning("Bind Build Bar Fail. tier must be 1 or 2.");
                        return false;
                    }
                }
                else
                {
                    BuildBarToolPlugin.logger.LogWarning("Bind Build Bar Fail. index must be between 1 and 10.");
                    return false;
                }
            }
            else
            {
                BuildBarToolPlugin.logger.LogWarning("Bind Build Bar Fail. category must be between 1 and 12.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Bind an item to a button on the build bar. 将一个物品绑定到建造栏的按钮上。
        /// </summary>
        /// <param name="proto"></param>
        /// <param name="buildIndex">BuildIndex = category * 100 + index</param>
        /// <param name="isTopRow">Bind the item to the buttons in top row (true) or bottom row (false). 将建筑绑定在上面一行(truw)还是下面一行(false)的按钮上。</param>
        public static void SetBuildBar(this ItemProto proto, int buildIndex, bool isTopRow)
        {
            SetBuildBar( buildIndex / 100, buildIndex % 100, proto.ID, isTopRow);
            if(isTopRow)
                proto.BuildIndex = 0;
        }

        /// <summary>
        /// Bind an item to a button on the build bar. 将一个物品绑定到建造栏的按钮上。
        /// </summary>
        /// <param name="proto"></param>
        /// <param name="category">Category that the button belongs to (from left to right: 1-10). 按钮所在的建造栏的类别序号（1-10）。</param>
        /// <param name="index">Button position (from left to right: 1-10). 按钮位置序号（从左向右1-10）。</param>
        /// <param name="isTopRow">Bind the item to the buttons in top row (true) or bottom row (false). 将建筑绑定在上面一行(truw)还是下面一行(false)的按钮上。</param>
        public static void SetBuildBar(this ItemProto proto, int category, int index, bool isTopRow)
        {
            SetBuildBar(category, index, proto.ID, isTopRow);
            if(isTopRow)
                proto.BuildIndex = 0;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="proto"></param>
        //public static void SetToTier2BuildBar(this ItemProto proto)
        //{
        //    Bind(proto.BuildIndex/100, proto.BuildIndex % 100, proto.ID, 2);
        //    proto.BuildIndex = 0;
        //}

    }
}
