
//复制生成新文件
var dupoc = app.activeDocument.duplicate();
CheckLayer(dupoc);
dupoc.close(SaveOptions.SAVECHANGES)
//整理所有图层
function CheckLayer(layer){
    if(typeof(layer) == "undefined"){
        return ;
    }
    if (typeof(layer.layers) != "undefined" && layer.layers.length>0) {

        for (var i = layer.layers.length - 1; 0 <= i; i--)
        {
            CheckLayer(layer.layers[i])
        }
    }
    if (layer.name.search("=png")>=0)
    {
        ConvertToSmartObject(layer)
    }else{
        if(layer.typename == "LayerSet"){
           
        }else{
            if (LayerKind.TEXT == layer.kind)
            {
                CheckText(layer);
            }else{
                //alert(layer.kind)
            }
        }
    }
}
//转换图层为智能对象
function ConvertToSmartObject(layer){
    var name=layer.name;
    layer.name="#@!temp"
    var idslct = charIDToTypeID("slct");
    var desc9 = new ActionDescriptor();
    var idnull = charIDToTypeID("null");
    var ref4 = new ActionReference();
    var idLyr = charIDToTypeID("Lyr ");
    ref4.putName(idLyr, layer.name);
    desc9.putReference(idnull, ref4);
    var idMkVs = charIDToTypeID("MkVs");
    desc9.putBoolean(idMkVs, false);
    var idLyrI = charIDToTypeID("LyrI");
    var list1 = new ActionList();
    list1.putInteger(4);
    desc9.putList(idLyrI, list1);
    executeAction(idslct, desc9);
    var idnewPlacedLayer = stringIDToTypeID("newPlacedLayer");
    executeAction(idnewPlacedLayer, undefined, DialogModes.NO);
    app.activeDocument.activeLayer.name=name
}
function CheckText(layer){
}