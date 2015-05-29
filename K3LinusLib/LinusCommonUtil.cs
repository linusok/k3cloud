using Kingdee.BOS;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Enums;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ControlElement;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldConverter;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Core.Metadata.Util;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.Metadata.ElementMetadata;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.Exceptions;
using Kingdee.BOS.Resource;
using System.Collections;

namespace K3LinusLib
{
    /// <summary>
    /// 通用工具类
    /// </summary>
    public static class LinusCommonUtil
    {
        #region clr类型与db类型互相转换
        /// <summary>
        /// 将金蝶的数据类型转换成clr类型
        /// </summary>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public static Type ToClrType(this KDDbType dbType)
        {
            switch (dbType)
            {
                case KDDbType.Boolean:
                    return typeof(bool);

                case KDDbType.Date:
                case KDDbType.DateTime:
                case KDDbType.Time:
                case KDDbType.DateTime2:
                    return typeof(DateTime);

                case KDDbType.Decimal:
                    return typeof(decimal);

                case KDDbType.Double:
                    return typeof(double);

                case KDDbType.Guid:
                    return typeof(Guid);

                case KDDbType.Int16:
                case KDDbType.UInt16:
                    return typeof(short);

                case KDDbType.Int32:
                case KDDbType.UInt32:
                    return typeof(int);

                case KDDbType.Int64:
                case KDDbType.UInt64:
                    return typeof(long);
            }
            return typeof(string);
        }


        /// <summary>
        /// 把CLR类型转换成对应的数据库类型
        /// </summary>
        /// <param name="clrType"></param>
        /// <returns></returns>
        public static KDDbType ToDbType(this Type clrType)
        {
            if ((clrType == typeof(short)) || (clrType == typeof(ushort)))
            {
                return KDDbType.Int16;
            }
            if ((clrType == typeof(int)) || (clrType == typeof(uint)))
            {
                return KDDbType.Int32;
            }
            if ((clrType == typeof(long)) || (clrType == typeof(ulong)))
            {
                return KDDbType.Int64;
            }
            if ((clrType == typeof(decimal)) || (clrType == typeof(float)))
            {
                return KDDbType.Decimal;
            }
            if (clrType == typeof(double))
            {
                return KDDbType.Double;
            }
            if (clrType == typeof(bool))
            {
                return KDDbType.Boolean;
            }
            if (clrType == typeof(DateTime))
            {
                return KDDbType.DateTime;
            }
            if (clrType == typeof(Guid))
            {
                return KDDbType.Guid;
            }
            return KDDbType.String;
        }
        #endregion

        public static T CreateElement<T, K>(Context ctx, string key, string caption, string fieldOrTableName = "", ElementType elementType = null)
            where T : ControlAppearance, new()
            where K : Element, new()
        {
            T ap = Activator.CreateInstance<T>();
            if (elementType != null)
            {
                PropertyUtil.SetAppearenceDefaultValue(ap, elementType, ctx.UserLocale.LCID);
            }

            ap.Key = key;

            if (ap is FieldAppearance)
            {
                object item = Activator.CreateInstance<K>();
                (ap as FieldAppearance).Field = (Field)item;
                if (elementType != null)
                {
                    PropertyUtil.SetBusinessDefaultValue((ap as FieldAppearance).Field, elementType, ctx.UserLocale.LCID);
                }

                (ap as FieldAppearance).Field.Key = key;

                (ap as FieldAppearance).Field.Name = ap.Caption;
                (ap as FieldAppearance).Field.FieldName = fieldOrTableName;
                (ap as FieldAppearance).Field.PropertyName = string.IsNullOrWhiteSpace(fieldOrTableName) ? key : fieldOrTableName;

                (ap as FieldAppearance).Field.FireUpdateEvent = 1;
            }
            else if (ap is EntityAppearance)
            {
                object item = Activator.CreateInstance<K>();
                (ap as EntityAppearance).Entity = (Entity)item;
                if (elementType != null)
                {
                    PropertyUtil.SetBusinessDefaultValue((ap as EntityAppearance).Entity, elementType, ctx.UserLocale.LCID);
                }

                (ap as EntityAppearance).Entity.Key = key;

                (ap as EntityAppearance).Entity.Name = ap.Caption;
                (ap as EntityAppearance).Entity.TableName = fieldOrTableName;
                (ap as EntityAppearance).Entity.EntryName = string.IsNullOrWhiteSpace(fieldOrTableName) ? key : fieldOrTableName;
            }

            ap.Caption = new LocaleValue(caption, ctx.UserLocale.LCID);
            ap.Width = new LocaleValue("100", ctx.UserLocale.LCID);

            ap.Locked = -1;
            ap.Visible = -1;

            return ap;
        }

        /// <summary>
        /// 实现多个集合的迪卡尔积
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequences"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> CartesianJoin<T>(this IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
            return sequences.Aggregate(
              emptyProduct,
              (accumulator, sequence) =>
                from accseq in accumulator
                from item in sequence
                select accseq.Concat(new[] { item }));
        }

        /// <summary>
        /// 两个对象比较大小
        /// </summary>
        /// <param name="o1"></param>
        /// <param name="o2"></param>
        /// <returns></returns>
        public static int Compare(this object o1, object o2)
        {
            if (o1 == null)
            {
                if (o2 != null)
                {
                    return -1;
                }
                return 0;
            }
            if (o2 == null)
            {
                return 1;
            }
            if ((o1 is string) && (o2 is string))
            {
                return string.Compare((string)o1, (string)o2, StringComparison.CurrentCultureIgnoreCase);
            }
            IComparable comparable = o1 as IComparable;
            if (comparable != null)
            {
                return comparable.CompareTo(o2);
            }
            return 0;
        }

        #region 显示一个表单或列表
        /// <summary>
        /// 显示表单
        /// </summary>
        /// <param name="view"></param>
        /// <param name="panelKey"></param>
        /// <returns></returns>
        public static void ShowForm(this IDynamicFormView view, string formId, string panelKey = null, string pageId = null, Action<FormResult> callback = null, Action<DynamicFormShowParameter> showParaCallback = null)
        {
            DynamicFormShowParameter showPara = new DynamicFormShowParameter();
            showPara.PageId = string.IsNullOrWhiteSpace(pageId) ? Guid.NewGuid().ToString() : pageId;
            showPara.ParentPageId = view.PageId;
            if (string.IsNullOrWhiteSpace(panelKey))
            {
                showPara.OpenStyle.ShowType = ShowType.Default;
            }
            else
            {
                showPara.OpenStyle.ShowType = ShowType.InContainer;
                showPara.OpenStyle.TagetKey = panelKey;
            }
            showPara.FormId = formId;
            showPara.OpenStyle.CacheId = pageId;
            if (showParaCallback != null)
            {
                showParaCallback(showPara);
            }

            view.ShowForm(showPara, callback);
        }

        /// <summary>
        /// 显示列表
        /// </summary>
        /// <param name="view"></param>
        /// <param name="formId"></param>
        /// <param name="listType"></param>
        /// <param name="bMultiSel"></param>
        /// <param name="callback"></param>
        public static void ShowList(this IDynamicFormView view, string formId, BOSEnums.Enu_ListType listType, bool bMultiSel = true, string filter = "", Action<ListShowParameter> showPara = null, Action<FormResult> callback = null)
        {
            ListShowParameter listShowPara = new ListShowParameter();
            listShowPara.FormId = formId;
            listShowPara.PageId = Guid.NewGuid().ToString();
            listShowPara.ParentPageId = view.PageId;
            listShowPara.MultiSelect = bMultiSel;
            listShowPara.ListType = (int)listType;
            if (listType == BOSEnums.Enu_ListType.SelBill)
            {
                listShowPara.IsLookUp = true;
            }
            listShowPara.ListFilterParameter.Filter = listShowPara.ListFilterParameter.Filter.JoinFilterString(filter);
            listShowPara.IsShowUsed = true;
            listShowPara.IsShowApproved = false;
            if (showPara != null) showPara(listShowPara);

            view.ShowForm(listShowPara, callback);
        }
        #endregion

        #region 设置表单元素的可用性及可见性
        /// <summary>
        /// 设置某个实体整体可用性
        /// </summary>
        /// <param name="view"></param>
        /// <param name="entityKey"></param>
        /// <param name="bEnabled"></param>
        public static void SetEntityEnabled(this IDynamicFormView view, string entityKey, bool bEnabled)
        {
            EntityAppearance entityAp = view.LayoutInfo.GetEntityAppearance(entityKey);
            if (entityAp == null) return;
            foreach (var ap in entityAp.Layoutinfo.Controls)
            {
                view.StyleManager.SetEnabled(ap, null, bEnabled);
            }
        }

        /// <summary>
        /// 设置行可用性
        /// </summary>
        /// <param name="view"></param>
        /// <param name="entityKey"></param>
        /// <param name="row"></param>
        /// <param name="bEnabled"></param>
        /// <param name="exceptFieldkeys"></param>
        public static void SetEntityRowEnabled(this IDynamicFormView view, string entityKey, int row, bool bEnabled, IEnumerable<string> exceptFieldkeys = null)
        {
            Entity entity = view.BillBusinessInfo.GetEntity(entityKey);
            DynamicObject rowObj = view.Model.GetEntityDataObject(entity, row);

            SetEntityRowEnabled(view, entityKey, rowObj, bEnabled, exceptFieldkeys);
        }

        /// <summary>
        /// 设置行可用性
        /// </summary>
        /// <param name="view"></param>
        /// <param name="entityKey"></param>
        /// <param name="rowObject"></param>
        /// <param name="bEnabled"></param>
        /// <param name="exceptFieldkeys"></param>
        public static void SetEntityRowEnabled(this IDynamicFormView view, string entityKey, DynamicObject rowObject, bool bEnabled, IEnumerable<string> exceptFieldkeys = null)
        {
            if (exceptFieldkeys == null) exceptFieldkeys = new string[] { };

            foreach (Field field in (from o in view.BillBusinessInfo.GetEntryEntity(entityKey).Fields
                                     where !exceptFieldkeys.Contains(o.Key)
                                     select o).ToList<Field>())
            {
                if (field is RelatedFlexGroupField)
                {
                    view.SetFlexFieldEnabled(field.Key, rowObject, bEnabled);
                }
                else
                {
                    view.StyleManager.SetEnabled(field, rowObject, null, bEnabled);
                }
            }
        }

        /// <summary>
        /// 设置维度属性字段的锁定性
        /// </summary>
        /// <param name="view"></param>
        /// <param name="flexFieldKey"></param>
        /// <param name="rowObject"></param>
        /// <param name="bEnabled"></param>
        public static void SetFlexFieldEnabled(this IDynamicFormView view, string flexFieldKey, DynamicObject rowObject, bool bEnabled)
        {
            RelatedFlexGroupField flexField = view.BillBusinessInfo.GetField(flexFieldKey) as RelatedFlexGroupField;
            if (flexField == null) return;
            if (flexField.FlexDisplayFormat == FlexType.Format.POPUPBOX)
            {
                //this.View.StyleManager.SetVisible(controlFieldKey, null, bVisible);
                if (rowObject != null)
                {
                    view.StyleManager.SetEnabled(flexField, rowObject, null, bEnabled);
                }
                else
                {
                    view.StyleManager.SetEnabled(flexField, null, bEnabled);
                }
            }
            else
            {
                foreach (var itemField in flexField.RelateFlexBusinessInfo.GetFieldList())
                {
                    string strColKey = string.Format("$${0}__{1}", flexField.Key, itemField.Key);
                    if (rowObject != null)
                    {
                        view.StyleManager.SetEnabled(strColKey, rowObject, null, bEnabled);
                    }
                    else
                    {
                        view.StyleManager.SetEnabled(strColKey, null, bEnabled);
                    }
                }
            }
        }

        public static void SetFlexFieldVisible(this IDynamicFormView view, string flexFieldKey, bool bVisible)
        {
            RelatedFlexGroupField flexField = view.BillBusinessInfo.GetField(flexFieldKey) as RelatedFlexGroupField;
            if (flexField == null) return;
            if (flexField.FlexDisplayFormat == FlexType.Format.POPUPBOX)
            {
                //this.View.StyleManager.SetVisible(controlFieldKey, null, bVisible);
                view.StyleManager.SetVisible(flexField.Key, null, bVisible);
            }
            else
            {
                foreach (var itemField in flexField.RelateFlexBusinessInfo.GetFieldList())
                {
                    string strColKey = string.Format("$${0}__{1}", flexField.Key, itemField.Key);
                    view.StyleManager.SetVisible(strColKey, null, bVisible);
                }
            }
        }

        /// <summary>
        /// 设置工具条整体可用状态
        /// </summary>
        /// <param name="view"></param>
        /// <param name="bEnabled">可用性</param>
        /// <param name="barOwnerKey">工具条拥有者标识，单据主工具条不用传值，表格工具条请传表格标识，其它独立工具条请传工具条标识</param>
        public static void SetBarEnabled(this IDynamicFormView view, bool bEnabled, string barOwnerKey = "")
        {
            if (string.IsNullOrWhiteSpace(barOwnerKey))
            {
                FormAppearance formAppearance = view.LayoutInfo.GetFormAppearance();
                if (((formAppearance.Menu != null) && !string.IsNullOrWhiteSpace(formAppearance.Menu.Id)) && (formAppearance.ShowMenu == 1))
                {
                    foreach (var item in formAppearance.Menu.GetAllBarItems())
                    {
                        view.SetBarItemEnabled(item.Key, bEnabled, barOwnerKey);
                    }
                }
            }
            else
            {
                EntryEntityAppearance appearance3 = view.LayoutInfo.GetEntryEntityAppearance(barOwnerKey);
                if ((appearance3 != null) && (appearance3.Menu != null))
                {
                    foreach (var item in appearance3.Menu.GetAllBarItems())
                    {
                        view.SetBarItemEnabled(item.Key, bEnabled, barOwnerKey);
                    }
                }

                ToolBarCtrlAppearance appearance4 = view.LayoutInfo.GetToolbarCtrlAppearances().FirstOrDefault(o => o.Key == barOwnerKey);
                if ((appearance4 != null) && (appearance4.Menu != null))
                {
                    foreach (var item in appearance4.Menu.GetAllBarItems())
                    {
                        view.SetBarItemEnabled(item.Key, bEnabled, barOwnerKey);
                    }
                }
            }

        }

        /// <summary>
        /// 设置某个菜单条的可见性
        /// </summary>
        /// <param name="view"></param>
        /// <param name="bVisible">可见性</param>
        /// <param name="barOwnerKey">工具条拥有者标识，单据主工具条不用传值，表格工具条请传表格标识，其它独立工具条请传工具条标识</param>
        public static void SetBarVisible(this IDynamicFormView view, bool bVisible, string barOwnerKey = "")
        {
            if (string.IsNullOrWhiteSpace(barOwnerKey))
            {
                FormAppearance formAppearance = view.LayoutInfo.GetFormAppearance();
                if (((formAppearance.Menu != null) && !string.IsNullOrWhiteSpace(formAppearance.Menu.Id)) && (formAppearance.ShowMenu == 1))
                {
                    foreach (var item in formAppearance.Menu.GetAllBarItems())
                    {
                        view.SetBarItemVisible(item.Key, bVisible, barOwnerKey);
                    }
                }
            }
            else
            {
                EntryEntityAppearance appearance3 = view.LayoutInfo.GetEntryEntityAppearance(barOwnerKey);
                if ((appearance3 != null) && (appearance3.Menu != null))
                {
                    foreach (var item in appearance3.Menu.GetAllBarItems())
                    {
                        view.SetBarItemVisible(item.Key, bVisible, barOwnerKey);
                    }
                }

                ToolBarCtrlAppearance appearance4 = view.LayoutInfo.GetToolbarCtrlAppearances().FirstOrDefault(o => o.Key == barOwnerKey);
                if ((appearance4 != null) && (appearance4.Menu != null))
                {
                    foreach (var item in appearance4.Menu.GetAllBarItems())
                    {
                        view.SetBarItemVisible(item.Key, bVisible, barOwnerKey);
                    }
                }
            }

        }

        /// <summary>
        /// 设置按钮可用状态
        /// </summary>
        /// <param name="view"></param>
        /// <param name="barItemKey">按钮标识</param>
        /// <param name="bEnabled">可用性</param>
        /// <param name="barOwnerKey">工具条拥有者标识，单据主工具条不用传值，表格工具条请传表格标识，其它独立工具条请传工具条标识</param>
        public static void SetBarItemEnabled(this IDynamicFormView view, string barItemKey, bool bEnabled, string barOwnerKey = "")
        {
            Appearance ap = null;
            if (!string.IsNullOrWhiteSpace(barOwnerKey))
                ap = view.LayoutInfo.GetAppearance(barOwnerKey);

            BarItemControl barItem = null;
            if (ap == null)
            {
                barItem = view.GetMainBarItem(barItemKey);

                if (barItem != null)
                {
                    barItem.Enabled = bEnabled;
                }
            }

            foreach (var entityAp in view.LayoutInfo.GetEntityAppearances())
            {
                if (entityAp is HeadEntityAppearance || entityAp is SubHeadEntityAppearance) continue;

                if (barOwnerKey.IsNullOrEmptyOrWhiteSpace() || entityAp.Key.EqualsIgnoreCase(barOwnerKey))
                {
                    barItem = view.GetBarItem(entityAp.Key, barItemKey);

                    if (barItem != null)
                    {
                        barItem.Enabled = bEnabled;
                    }
                }
            }

        }

        /// <summary>
        /// 设置按钮可见状态
        /// </summary>
        /// <param name="view"></param>
        /// <param name="barItemKey">按钮标识</param>
        /// <param name="bVisible">可见性</param>
        /// <param name="barOwnerKey">工具条拥有者标识，单据主工具条不用传值，表格工具条请传表格标识，其它独立工具条请传工具条标识</param>
        public static void SetBarItemVisible(this IDynamicFormView view, string barItemKey, bool bVisible, string barOwnerKey = "")
        {
            Appearance ap = null;
            if (!string.IsNullOrWhiteSpace(barOwnerKey))
                ap = view.LayoutInfo.GetAppearance(barOwnerKey);

            BarItemControl barItem = null;
            if (ap == null)
            {
                barItem = view.GetMainBarItem(barItemKey);
            }
            else
            {
                barItem = view.GetBarItem(ap.Key, barItemKey);
            }
            if (barItem != null)
            {
                barItem.Visible = bVisible;
            }
        }
        #endregion

        /// <summary>
        /// 更新可视元素宽度
        /// </summary>
        /// <param name="formState"></param>
        /// <param name="key"></param>
        /// <param name="width"></param>
        public static void UpdateColumnWidth(this IDynamicFormView view, ControlAppearance gridAp, string colKey, int width)
        {
            IDynamicFormState formState = view.GetService<IDynamicFormState>();
            //SetFieldPropValue(formState, ctlAp.Key, "width", width, -1);
            SetColumnPropValue(formState, gridAp, colKey, "width", width);
        }

        public static void UpdateColumnHeader(this IDynamicFormView view, ControlAppearance gridAp, string colKey, string header)
        {
            IDynamicFormState formState = view.GetService<IDynamicFormState>();
            SetColumnPropValue(formState, gridAp, colKey, "header", header);
        }

        private static void SetFieldPropValue(IDynamicFormState formState, string key, string propName, object value, int rowIndex)
        {
            JSONObject obj2 = formState.GetControlProperty(key, -1, propName) as JSONObject;
            if (obj2 == null)
            {
                obj2 = new JSONObject();
            }
            obj2[rowIndex.ToString()] = value;
            formState.SetControlProperty(key, rowIndex, propName, obj2);
        }

        private static void SetColumnPropValue(IDynamicFormState formState, ControlAppearance ctlAp, string colKey, string propName, object value)
        {
            JSONObject obj2 = new JSONObject();
            obj2["key"] = colKey;
            obj2[propName] = value;
            formState.InvokeControlMethod(ctlAp, "UpdateFieldStates", new object[] { obj2 });
        }

        /// <summary>
        /// 移动表格分录
        /// </summary>
        /// <param name="view"></param>
        /// <param name="entityKey"></param>
        /// <param name="iSrcRowIndex"></param>
        /// <param name="iDstRowIndex"></param>
        /// <param name="callback"></param>
        public static void MoveEntryRow(this IDynamicFormView view, string entityKey, int iSrcRowIndex, int iDstRowIndex, Action<int, int> callback = null)
        {
            EntryEntity entryEntity = view.BillBusinessInfo.GetEntryEntity(entityKey);
            DynamicObjectCollection dataEntities = view.Model.GetEntityDataObject(entryEntity);
            if (iSrcRowIndex < 0 || iSrcRowIndex >= dataEntities.Count) return;
            if (iDstRowIndex < 0 || iDstRowIndex >= dataEntities.Count) return;
            var srcRow = dataEntities[iSrcRowIndex];
            var dstRow = dataEntities[iDstRowIndex];
            if (iSrcRowIndex > iDstRowIndex)
            {
                dataEntities.RemoveAt(iSrcRowIndex);
                dataEntities.Insert(iDstRowIndex, srcRow);
            }
            else
            {
                dataEntities.RemoveAt(iDstRowIndex);
                dataEntities.Insert(iSrcRowIndex, dstRow);
            }

            EntryGrid grid = view.GetControl<EntryGrid>(entityKey);
            grid.ExchangeRowIndex(iSrcRowIndex, iDstRowIndex);
            grid.SetFocusRowIndex(iDstRowIndex);

            if (callback != null)
            {
                callback(iSrcRowIndex, iDstRowIndex);
            }
        }

        /// <summary>
        /// 移动表格分录，保证模型与视图索引匹配
        /// </summary>
        /// <param name="view"></param>
        /// <param name="entityKey"></param>
        /// <param name="selRows"></param>
        /// <param name="iTargetPos"></param>
        /// <param name="isRelativePos"></param>
        /// <param name="callback"></param>
        public static void MoveEntryRow(this IDynamicFormView view, string entityKey, int[] selRows, int iTargetPos, bool isRelativePos = true, Action<int[], int, bool> callback = null)
        {
            if (selRows == null || selRows.Any() == false) return;

            var entryEntity = view.BillBusinessInfo.GetEntity(entityKey);
            DynamicObjectCollection dataEntities = view.Model.GetEntityDataObject(entryEntity);

            DynamicObject targetRowObj = null;
            if (!isRelativePos)
            {
                if (iTargetPos < 0 || iTargetPos >= dataEntities.Count) return;

                targetRowObj = view.Model.GetEntityDataObject(entryEntity, iTargetPos);
                if (targetRowObj == null) return;
            }

            var selRowObjItems = selRows.Select(o => view.Model.GetEntityDataObject(entryEntity, o))
                .ToList();

            List<int> lstNewSelRows = new List<int>();

            for (int i = selRowObjItems.Count - 1; i >= 0; i--)
            {
                if (selRowObjItems[i] == null) continue;

                var selRowObj = selRowObjItems[i];
                int iSrcRowIndex = view.Model.GetRowIndex(entryEntity, selRowObj);
                int iTargetRowIndex = iSrcRowIndex + iTargetPos;
                if (!isRelativePos)
                {
                    iTargetRowIndex = view.Model.GetRowIndex(entryEntity, targetRowObj);
                }

                if (iTargetRowIndex > dataEntities.Count - 1 || iTargetRowIndex < 0) continue;

                dataEntities.Remove(selRowObj);
                dataEntities.Insert(iTargetRowIndex, selRowObj);
                lstNewSelRows.Add(iTargetRowIndex);
            }

            int iSeq = 1;

            foreach (var dataEntity in dataEntities)
            {
                if (entryEntity.SeqDynamicProperty != null)
                {
                    entryEntity.SeqDynamicProperty.SetValue(dataEntity, iSeq++);
                }
            }

            if (callback != null)
            {
                callback(selRows, iTargetPos, isRelativePos);
            }

            view.UpdateView(entityKey);

            if (lstNewSelRows.IsEmpty()) lstNewSelRows.AddRange(selRows);
            if (lstNewSelRows.Any())
            {
                EntryGrid grid = view.GetControl<EntryGrid>(entityKey);
                grid.SelectRows(lstNewSelRows.ToArray());
                grid.SetFocusRowIndex(lstNewSelRows.Last());
            }

        }

        #region 表单事件交互辅助
        private static ConcurrentDictionary<string, string> dctFormInteractFlags = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// 把当前表单的事件通知给另一个表单
        /// </summary>
        /// <param name="srcFormView"></param>
        /// <param name="dstFormView"></param>        
        /// <param name="eventName"></param>
        /// <param name="eventData"></param>
        public static void SendFormInteractEvent(this IDynamicFormView srcFormView, IDynamicFormView dstFormView, string eventName, object eventData = null, bool bForceNoUpdateView = false)
        {
            if (dstFormView != null)
            {
                string eventDataJson = string.Empty;
                if (eventData != null)
                {
                    try
                    {
                        if ((eventData is string) || (eventData is ValueType))
                        {
                            eventDataJson = eventData.ToString();
                        }
                        else
                        {
                            eventDataJson = KDObjectConverter.SerializeObject(eventData);
                        }
                    }
                    catch
                    {
                        //无法序列化时，则使用父级Session传递参数对象
                        eventDataJson = string.Format("{0}_{1}", srcFormView.PageId, eventName);
                        dstFormView.Session[eventDataJson] = eventData;
                    }
                }
                try
                {
                    (dstFormView as IDynamicFormViewService).CustomEvents(srcFormView.BusinessInfo.GetForm().Id, eventName, eventDataJson);
                }
                catch (KDBusinessException ex)
                {
                    dstFormView.ShowMessage(ex.ToString());
                }
                finally
                {
                    if (IsNeedRedirectEventHandler(srcFormView, dstFormView) && !bForceNoUpdateView)
                    {
                        srcFormView.SendDynamicFormAction(dstFormView);
                    }
                }
            }
        }

        /// <summary>
        /// 开启一个表单交互
        /// </summary>
        /// <param name="srcFormView"></param>
        /// <param name="dstFormView"></param>
        /// <returns></returns>
        public static void BeginFormInteract(this IDynamicFormView srcFormView, IDynamicFormView dstFormView)
        {
            dctFormInteractFlags.AddOrUpdate(string.Format("{0}_{1}", srcFormView.PageId, dstFormView.PageId), "", (key, value) =>
            {
                throw new KDBusinessException("JN-BOS-000004", string.Format("源表单{0}与目标表单{1}正在数据交换，请稍候操作！", srcFormView.PageId, dstFormView.PageId));
            });
        }

        /// <summary>
        /// 结束表单交互
        /// </summary>
        /// <param name="srcFormView"></param>
        /// <param name="dstFormView"></param>
        public static void EndFormInteract(this IDynamicFormView srcFormView, IDynamicFormView dstFormView)
        {
            string key = string.Format("{0}_{1}", srcFormView.PageId, dstFormView.PageId);
            string value = null;
            dctFormInteractFlags.TryRemove(key, out value);
        }

        private static bool IsNeedRedirectEventHandler(IDynamicFormView srcFormView, IDynamicFormView dstFormView)
        {
            string pageId = null;
            return !dctFormInteractFlags.TryGetValue(string.Format("{0}_{1}", dstFormView.PageId, srcFormView.PageId), out pageId);
        }
        #endregion

        #region 实现块粘贴的填充功能
        /// <summary>
        /// 处理Excel块粘贴功能
        /// </summary>
        /// <param name="view"></param>
        /// <param name="e"></param>
        /// <param name="bAllowAutoNewRows">允许自动新增行</param>
        /// <param name="bCanPaste">是否允许填充某字段</param>
        public static void PasteBlockData(this IDynamicFormView view, EntityBlockPastingEventArgs e, bool bAllowAutoNewRows = false, Func<FieldAppearance, int, bool> bCanPaste = null)
        {
            if (e.BlockValue.IsNullOrEmptyOrWhiteSpace()) return;
            FieldAppearance startFieldAp = view.LayoutInfo.GetFieldAppearance(e.StartKey);
            if (startFieldAp == null || (startFieldAp.Field.Entity is EntryEntity) == false) return;
            EntryEntity entryEntity = (EntryEntity)startFieldAp.Field.Entity;
            int iTotalRows = view.Model.GetEntryRowCount(entryEntity.Key);

            var copyOperation = view.BillBusinessInfo.GetForm().FormOperations
                        .FirstOrDefault(o => o.OperationId == 31 && string.Equals(o.Parmeter.OperationObjectKey, entryEntity.Key, StringComparison.InvariantCultureIgnoreCase));
            bool isCopyLinkEntry = false;
            //如果表格未配置复制行操作，则不允许自动新增行
            if (copyOperation == null)
            {
                bAllowAutoNewRows = false;
            }
            else
            {
                isCopyLinkEntry = GetIsCopyLinkEntryParam(copyOperation.Parmeter);
            }

            string[] strBlockDataRows = e.BlockValue.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int iRow = e.StartRow;
            foreach (var rowData in strBlockDataRows)
            {
                if (iRow >= iTotalRows)
                {
                    if (bAllowAutoNewRows)
                    {
                        view.Model.CopyEntryRow(entryEntity.Key, iRow - 1, iRow, isCopyLinkEntry);
                    }
                    else
                    {
                        break;
                    }
                }
                string[] strItemValues = rowData.Split(new char[] { '\t' });

                FieldAppearance fieldAp = startFieldAp;
                foreach (var value in strItemValues)
                {
                    if (fieldAp == null) continue;
                    object objValue = value;

                    if (typeof(ValueType).IsAssignableFrom(fieldAp.Field.GetPropertyType()))
                    {
                        if (value.IsNullOrEmptyOrWhiteSpace())
                        {
                            objValue = 0;
                        }
                        else
                        {
                            ValueTypeConverter converter = new ValueTypeConverter();
                            if (value != null && converter.CanConvertTo(value.GetType()))
                            {
                                objValue = converter.ConvertTo(value, fieldAp.Field.GetPropertyType());
                            }
                        }
                    }
                    if (bCanPaste == null || bCanPaste(fieldAp, iRow))
                    {
                        (view as IDynamicFormViewService).UpdateValue(fieldAp.Key, iRow, objValue);
                    }
                    fieldAp = GetNextEditFieldAp(view, fieldAp, iRow);
                }

                iRow++;
            }
        }

        private static FieldAppearance GetNextEditFieldAp(IDynamicFormView view, FieldAppearance fieldAp, int iRow)
        {
            FieldAppearance nextFieldAp = null;
            if (fieldAp != null)
            {
                EntryEntityAppearance entryEntityAp = view.LayoutInfo.GetEntryEntityAppearance(fieldAp.EntityKey);
                if (entryEntityAp != null)
                {
                    DynamicObject rowData = view.Model.GetEntityDataObject(entryEntityAp.Entity, iRow);
                    int iStartFindPos = entryEntityAp.Layoutinfo.Appearances.IndexOf(fieldAp);
                    if (iStartFindPos >= 0)
                    {
                        for (int i = iStartFindPos + 1; i < entryEntityAp.Layoutinfo.Appearances.Count; i++)
                        {
                            nextFieldAp = entryEntityAp.Layoutinfo.Appearances[i] as FieldAppearance;
                            if (nextFieldAp == null) continue;
                            //跳过不可见或不可编辑的字段
                            if (nextFieldAp.IsLocked(view.OpenParameter.Status) == true
                                || nextFieldAp.IsVisible(view.OpenParameter.Status) == false) continue;

                            //单元格锁定也不填充
                            if (rowData != null && view.StyleManager.GetEnabled(fieldAp, rowData) == false) continue;

                            break;
                        }
                    }
                }
            }

            return nextFieldAp;
        }

        private static bool GetIsCopyLinkEntryParam(OperationParameter operationParameter)
        {
            bool flag = false;
            string expressValue = operationParameter.ExpressValue;
            if (!string.IsNullOrEmpty(expressValue))
            {
                string[] strArray = expressValue.Split(new char[] { ':' });
                if (strArray.Length == 2)
                {
                    flag = Convert.ToInt32(strArray[1]) == 1;
                }
            }
            return flag;
        }
        #endregion

        #region Reflect Util Tool
        /// <summary>
        /// 获取指定对象的指定属性的值
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propName"></param>
        /// <returns></returns>
        public static object GetPropertyValue(this object obj, string propName)
        {
            return PropertyUtil.GetPropertyValue(obj, propName);
        }

        /// <summary>
        /// 通过反射获取指定对象的指定属性值
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propName"></param>
        /// <param name="isNonPublic"></param>
        /// <returns></returns>
        public static object GetPropertyValue(this object obj, string propName, bool isNonPublic)
        {
            if (isNonPublic)
            {
                PropertyInfo propInfo = obj.GetType().GetProperty(propName);
                if (propInfo == null)
                    propInfo = obj.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.NonPublic);
                if (propInfo != null)
                {
                    propInfo.GetValue(obj, null);
                }
                return null;
            }
            else
            {
                return obj.GetPropertyValue(propName);
            }
        }

        public static void SetPropertyValue(this object obj, string propName, object value)
        {
            PropertyUtil.SetPropertyValue(obj, propName, value);
        }

        public static void SetPropertyValue(this object obj, string propName, object value, bool isNonPublic)
        {
            if (isNonPublic)
            {
                PropertyInfo propInfo = obj.GetType().GetProperty(propName);
                if (propInfo == null)
                    propInfo = obj.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.NonPublic);
                if (propInfo != null)
                {
                    propInfo.SetValue(obj, value, null);
                }
            }
            else
            {
                obj.SetPropertyValue(propName, value);
            }
        }

        /// <summary>
        /// 获取指定对象的指定字段的值
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static object GetFieldValue(this object obj, string fieldName)
        {
            if (obj == null) return null;
            return GetFieldValue(obj, fieldName, obj.GetType());
        }

        /// <summary>
        /// 获取指定对象的指定字段的值(搜索指定类型）
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="fieldName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object GetFieldValue(this object obj, string fieldName, Type type)
        {
            if (obj == null || type == null) return null;
            FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                return fieldInfo.GetValue(obj);
            }
            return null;
        }

        /// <summary>
        /// 设置指定对象的指定字段的值
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        public static void SetFieldValue(this object obj, string fieldName, object value)
        {
            if (obj == null) return;
            SetFieldValue(obj, fieldName, value, obj.GetType());
        }

        /// <summary>
        /// 设置指定对象的指定字段的值(指定基类型)
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        public static void SetFieldValue(this object obj, string fieldName, object value, Type type)
        {
            if (obj == null || type == null) return;
            FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(obj, value);
            }
        }
        #endregion

        #region Orm操作工具类        
        /// <summary>
        /// 把一个实体数据拷贝到另一个实体
        /// </summary>
        /// <param name="dataEntity"></param>
        /// <param name="newEntity"></param>
        /// <param name="copyHandler"></param>
        /// <param name="clearPrimaryKeyValue"></param>
        /// <param name="onlyDbProperty"></param>
        /// <param name="onlyDirtyProperty"></param>
        public static void CopyData(this IDataEntityBase dataEntity, IDataEntityBase newEntity, Func<string, string, bool> copyHandler = null, bool clearPrimaryKeyValue = false, bool onlyDbProperty = false, bool onlyDirtyProperty = false)
        {
            IDataEntityType dataEntityType = dataEntity.GetDataEntityType();
            IDataEntityType type2 = newEntity.GetDataEntityType();
            if (copyHandler == null)
            {
                copyHandler = (dtName, propName) => true;
            }
            IEnumerable<IDataEntityProperty> dirtyProperties = dataEntityType.GetDirtyProperties(dataEntity);
            foreach (ISimpleProperty property in type2.Properties.GetSimpleProperties(onlyDbProperty))
            {
                if (copyHandler(type2.Name, property.Name))
                {
                    IDataEntityProperty dpOldProperty = null;
                    TryGetOldProperty(property, dataEntityType, out dpOldProperty);
                    if ((!onlyDirtyProperty || dirtyProperties.Contains<IDataEntityProperty>(dpOldProperty)) && !(property.IsReadOnly || (dpOldProperty == null)))
                    {
                        property.SetValue(newEntity, dpOldProperty.GetValue(dataEntity));
                    }
                }
            }
            if (clearPrimaryKeyValue)
            {
                ISimpleProperty primaryKey = type2.PrimaryKey;
                if (primaryKey != null)
                {
                    primaryKey.ResetValue(newEntity);
                }
                type2.SetDirty(newEntity, true);
            }
            foreach (IComplexProperty property4 in type2.Properties.GetComplexProperties(onlyDbProperty))
            {
                IDataEntityProperty property5 = null;
                TryGetOldProperty(property4, dataEntityType, out property5);
                IDataEntityBase base2 = property5.GetValue(dataEntity) as IDataEntityBase;
                if (base2 != null)
                {
                    IDataEntityBase base3;
                    if (property4.IsReadOnly)
                    {
                        base3 = property4.GetValue(newEntity) as IDataEntityBase;
                        if (base3 == null)
                        {
                            throw new ORMDesignException("??????", ResManager.LoadKDString("哦，真不幸，只读的属性却返回了NULL值。", "014009000001633", SubSystemType.SL, new object[0]));
                        }
                        base2.CopyData(base3, copyHandler, false, onlyDbProperty, false);
                    }
                    else
                    {
                        base3 = property4.ComplexPropertyType.CreateInstance() as IDataEntityBase;
                        base2.CopyData(base3, copyHandler, clearPrimaryKeyValue, onlyDbProperty, false);
                        property4.SetValue(newEntity, base3);
                    }
                }
            }
            foreach (ICollectionProperty property6 in type2.Properties.GetCollectionProperties(onlyDbProperty))
            {
                IDataEntityProperty property7 = null;
                TryGetOldProperty(property6, dataEntityType, out property7);
                object obj2 = property7.GetValue(dataEntity);
                if (obj2 != null)
                {
                    IEnumerable enumerable2 = obj2 as IEnumerable;
                    if (enumerable2 == null)
                    {
                        throw new ORMDesignException("??????", ResManager.LoadKDString("哦，真不幸，集合的属性返回值不支持枚举。", "014009000001634", SubSystemType.SL, new object[0]));
                    }
                    object obj3 = property6.GetValue(newEntity);
                    if (obj3 == null)
                    {
                        if (property6.IsReadOnly)
                        {
                            throw new ORMDesignException("??????", ResManager.LoadKDString("哦，真不幸，集合的属性返回值为null。", "014009000001635", SubSystemType.SL, new object[0]));
                        }
                        obj3 = Activator.CreateInstance(property6.PropertyType);
                        property6.SetValue(newEntity, obj3);
                    }
                    IList list = obj3 as IList;
                    if (list == null)
                    {
                        throw new ORMDesignException("??????", ResManager.LoadKDString("哦，真不幸，集合的属性返回值不支持IList。", "014009000001636", SubSystemType.SL, new object[0]));
                    }
                    list.Clear();
                    foreach (IDataEntityBase base4 in enumerable2)
                    {
                        IDataEntityBase base5 = property6.CollectionItemPropertyType.CreateInstance() as IDataEntityBase;
                        base4.CopyData(base5, copyHandler, clearPrimaryKeyValue, onlyDbProperty, false);
                        list.Add(base5);
                    }
                }
            }
        }

        private static bool TryGetOldProperty(IDataEntityProperty dp, IDataEntityType dtOldData, out IDataEntityProperty dpOldProperty)
        {
            dpOldProperty = null;
            return (((dtOldData != null) && (dp != null)) && dtOldData.Properties.TryGetValue(dp.Name, out dpOldProperty));
        } 
        #endregion
    }
}
