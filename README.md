k3cloud
=======

金蝶k3cloud产品二开共享库

author:linus_wang
createdate:2014-12-21

当前开源库旨在为K3Cloud的二开用户提供一组高度抽象及封装的一组工具库
1、K3LinusLib库定位于N层架构中的通用库，其中提炼的工具类及所有方法皆不涉及到数据库的访问
2、K3LinusViewLib库定位于视图层的工具库，其中提炼的方法仅适用于K3Cloud的业务层客户端插件

使用开源库的同学请注意，如何可以成功编译K3LinusLib库：
1、同步当前工程代码至本地后
2、需要从K3Cloud安装目录下复制如下几个文件至当前工程目录下：...\bin
   a:Kingdee.BOS.dll                            //金蝶基础库
   b:Kingdee.BOS.Core.dll                       //金蝶核心模型库
   c:Kingdee.BOS.DataEntity.dll                 //金蝶ORM模型库
   d:...其他工程需要的金蝶组件
3、经过上述步骤后，当前工程应该可以编译成功，编译后可以直接被K3Cloud的二开工程引用

欢迎其他有志为本项目做贡献的同学积极参与进来，可以把github账号发到我的邮箱里申请参与:yulin.1298@163.com 或 8816927@qq.com