using Cysharp.Threading.Tasks;
using ObservableCollections;
using Obvious.Soap;
using R3;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DataEnum;

public class TimelineManager : MonoBehaviour
{
    [SerializeField] private GameObject bannerArea;
    [SerializeField] private Banner bannerPrefab;
    [SerializeField] private IntVariable MaxBannerIndex;

    private TimelineManagerModel _timelineManagerModel;
    private TimelinePublisher _timelinePublisher;

    ICombatTextManager textManager;

    public void Init()
    {
        _timelineManagerModel = new TimelineManagerModel();
        _timelinePublisher = new TimelinePublisher();

        ServiceLocator.For(this).Get(out textManager);
    }

    public void CreateTimeline(List<BaseUnit> units)
    {
        while (_timelineManagerModel.BannerList.Count < MaxBannerIndex && units != null && units.Count > 0)
        {
            CreateBanners(units);
            _timelineManagerModel.SortBanners();
        }
    }

    public void Prepare(List<BaseUnit> units)
    {
        // Data Bindings
        _timelineManagerModel.BannerList.ObserveRemove()
            .Subscribe(Banner =>
            {
                if (_timelineManagerModel.BannerList.Count < MaxBannerIndex)
                {
                    CreateBanners(units);
                    _timelineManagerModel.SortBanners();
                }
            })
            .AddTo(this);

        _timelineManagerModel.CurRound.DistinctUntilChanged()
            .Subscribe(round =>
            {
                _timelinePublisher.UpdateRoundObservers(round);
                textManager.ShowNextRoundText(round);
            })
            .AddTo(this);

        foreach (var unit in units)
        {
            unit.CCUnit.EffectsCountDic[ELEMENT_TYPE.PHYSICAL].DistinctUntilChanged()
                .Subscribe(count =>
                {
                    if (count > 0)
                    {
                        var list = _timelineManagerModel.BannerList.Where(b => b.CompareStat(unit.GetStat())).ToList();
                        foreach (var banner in list)
                        {
                            banner.SetState(new BannerFaint(true));
                        }
                    }
                    else
                    {
                        var list = _timelineManagerModel.BannerList.Where(b => b.CompareStat(unit.GetStat())).ToList();
                        foreach (var banner in list)
                        {
                            banner.SetState(new BannerFaint(false));
                        }
                    }
                })
                .AddTo(this);
        }
    }

    public BaseUnit Pop(List<BaseUnit> unitList)
    {
        return _timelineManagerModel.OnPop(unitList);
    }

    private void CreateBanners(List<BaseUnit> units)
    {
        _timelineManagerModel.IncreaseRoundDepth();
        foreach (var unit in units)
        {
            NormalBanner banner = Instantiate(bannerPrefab).GetComponent<NormalBanner>();
            banner.transform.SetParent(bannerArea.transform, false);
            _timelineManagerModel.AddBanner(banner, unit);

            unit.GetStat().ModifierStat.Mediator.OnStatChanged += () => _timelineManagerModel.SortBanners();
        }
    }

    public void DeleteBanners(BaseUnit unit)
    {
        var bannersToRemove = _timelineManagerModel.BannerList.Where(banner => banner.CompareStat(unit.GetStat())).ToList();
        foreach (var banner in bannersToRemove)
        {
            _timelineManagerModel.RemoveBanner(banner);

            unit.GetStat().ModifierStat.Mediator.OnStatChanged -= () => _timelineManagerModel.SortBanners();
        }
    }
}
