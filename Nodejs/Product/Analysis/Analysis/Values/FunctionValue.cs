﻿//*********************************************************//
//    Copyright (c) Microsoft. All rights reserved.
//    
//    Apache 2.0 License
//    
//    You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
//    
//    Unless required by applicable law or agreed to in writing, software 
//    distributed under the License is distributed on an "AS IS" BASIS, 
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or 
//    implied. See the License for the specific language governing 
//    permissions and limitations under the License.
//
//*********************************************************//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.NodejsTools.Parsing;

namespace Microsoft.NodejsTools.Analysis.Values {
    [Serializable]
    internal class FunctionValue : ObjectValue {
        internal readonly InstanceValue _instance;
        internal ReferenceDict _references;

        internal FunctionValue(ProjectEntry projectEntry, ExpandoValue prototype = null, string name = null)
            : base(projectEntry, projectEntry.Analyzer._functionPrototype) {
            if (prototype == null) {
                string description = null;
#if DEBUG
                if (String.IsNullOrWhiteSpace(Name ?? name)) {
                    if (AnalysisUnit != null) {
                        var loc = Locations.First();
                        description = "prototype object of " + AnalysisUnit.FullName + " " + loc.FilePath + "(" + loc.Column + ")";
                    }
                    else {
                        description = "prototype object of <unknown objects>";
                    }
                }
                else {
                    description = "prototype object of " + (Name ?? name);
                }
#endif
                prototype = new PrototypeValue(ProjectEntry, this, description: description);
            }
            Add("prototype", prototype.Proxy);
            prototype.Add("constructor", this.Proxy);
            _instance = new InstanceValue(ProjectEntry, prototype, name);
        }

        public virtual IAnalysisSet ReturnTypes {
            get {
                return AnalysisSet.Empty;
            }
        }

        public IAnalysisSet NewThis {
            get {
                return _instance.SelfSet;
            }
        }

        public override string ShortDescription {
            get {
                return "function";
            }
        }

        public override void SetMember(Node node, AnalysisUnit unit, string name, IAnalysisSet value) {
            if (name == "prototype") {
                _instance.SetMember(node, unit, "__proto__", value);
            }
            base.SetMember(node, unit, name, value);
        }

        public override IAnalysisSet Construct(Node node, AnalysisUnit unit, IAnalysisSet[] args) {
            var result = Call(node, unit, _instance.Proxy, args);
            if (result.Count != 0) {
                // function returned a value, we want to return any values
                // which are typed to object.
                foreach (var resultValue in result) {
                    if (!resultValue.Value.IsObject) {
                        // we need to do some filtering
                        var tmpRes = AnalysisSet.Empty;
                        foreach (var resultValue2 in result) {
                            if (resultValue2.Value.IsObject) {
                                tmpRes = tmpRes.Add(resultValue2);
                            }
                        }
                        result = tmpRes;
                        break;
                    }
                }

                if (result.Count != 0) {
                    return result;
                }
            }
            // we didn't return a value or returned a non-object
            // value.  The result is our newly created instance object.
            return _instance.Proxy;
        }

        internal override Dictionary<string, IAnalysisSet> GetAllMembers(ProjectEntry accessor) {
            var res = base.GetAllMembers(accessor);

            if (this != ProjectState._functionPrototype) {
                foreach (var keyValue in ProjectState._functionPrototype.GetAllMembers(accessor)) {
                    IAnalysisSet existing;
                    if (!res.TryGetValue(keyValue.Key, out existing)) {
                        res[keyValue.Key] = keyValue.Value;
                    } else {
                        res[keyValue.Key] = existing.Union(keyValue.Value);
                    }
                }
            }

            return res;
        }

        public override IAnalysisSet Get(Node node, AnalysisUnit unit, string name, bool addRef = true) {
            var res = base.Get(node, unit, name, addRef);
            // we won't recurse on prototype because we have a prototype
            // value, and it's correct.  Recursing on prototype results in
            // prototypes getting merged and the analysis bloating
            if (this != ProjectState._functionPrototype && name != "prototype") {
                res = res.Union(ProjectState._functionPrototype.Get(node, unit, name));
            }
            return res;
        }

        public override JsMemberType MemberType {
            get {
                return JsMemberType.Function;
            }
        }

        internal static string MakeParameterName(ParameterDeclaration curParam) {
            return curParam.Name;
        }

        internal override void AddReference(Node node, AnalysisUnit unit) {
            if (!unit.ForEval) {
                if (_references == null) {
                    _references = new ReferenceDict();
                }
                _references.GetReferences(unit.DeclaringModuleEnvironment.ProjectEntry)
                    .AddReference(node.EncodedSpan);
            }
        }

        internal override IEnumerable<LocationInfo> References {
            get {
                if (_references != null) {
                    return _references.AllReferences;
                }
                return new LocationInfo[0];
            }
        }

        public override BuiltinTypeId TypeId {
            get {
                return BuiltinTypeId.Function;
            }
        }
        
        internal override bool UnionEquals(AnalysisValue av, int strength) {
#if FALSE
            if (strength >= MergeStrength.ToObject) {
                return av is FunctionValue /*|| av is BuiltinFunctionInfo || av == ProjectState.ClassInfos[BuiltinTypeId.Function].Instance*/;
            }
#endif
            return base.UnionEquals(av, strength);
        }

        internal override int UnionHashCode(int strength) {
#if FALSE
            if (strength >= MergeStrength.ToObject) {
                return ProjectState._numberPrototype.GetHashCode();
            }
#endif
            return base.UnionHashCode(strength);
        }

        internal override AnalysisValue UnionMergeTypes(AnalysisValue av, int strength) {
#if FALSE
            if (strength >= MergeStrength.ToObject) {
                return ProjectState.ClassInfos[BuiltinTypeId.Function].Instance;
            }
#endif

            return base.UnionMergeTypes(av, strength);
        }
    }
}
